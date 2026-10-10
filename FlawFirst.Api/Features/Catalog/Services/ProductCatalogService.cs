using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Catalog.Services;

public interface IProductCatalogService
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<ProductDetailDto> GetDetailAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<ReviewDto>> GetReviewsAsync(int productId, ReviewFilterQuery query, CancellationToken cancellationToken);
    Task<FlawExtractionDto> GetFlawsAsync(int productId, CancellationToken cancellationToken);
}

public class ProductCatalogService : IProductCatalogService
{
    private readonly AppDbContext _db;

    public ProductCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
            throw new AppException(StatusCodes.Status400BadRequest, "Khoảng giá không hợp lệ.");

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, 12);
        IQueryable<Product> products = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            products = products.Where(product =>
                product.Name.Contains(keyword) ||
                product.Brand.Contains(keyword) ||
                product.Category.Name.Contains(keyword));
        }

        if (query.CategoryId.HasValue)
            products = products.Where(product => product.CategoryId == query.CategoryId.Value);

        if (query.MinPrice.HasValue)
            products = products.Where(product => product.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            products = products.Where(product => product.Price <= query.MaxPrice.Value);

        products = (query.Sort ?? "newest").Trim().ToLowerInvariant() switch
        {
            "rating" => products
                .OrderByDescending(product => product.Reviews.Where(review => !review.IsHidden).Average(review => (double?)review.Rating) ?? 0)
                .ThenByDescending(product => product.Id),
            "mostreviewed" => products
                .OrderByDescending(product => product.Reviews.Count(review => !review.IsHidden))
                .ThenByDescending(product => product.Id),
            _ => products.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id)
        };

        var total = await products.CountAsync(cancellationToken);
        var items = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductListItemDto
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                AvgRating = product.Reviews.Where(review => !review.IsHidden).Average(review => (double?)review.Rating) ?? 0,
                TotalReviews = product.Reviews.Count(review => !review.IsHidden),
                CreatedAt = product.CreatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.AvgRating = Math.Round(item.AvgRating, 2);

        return new PagedResult<ProductListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = items
        };
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _db.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = category.Products.Count(),
                CreatedAt = category.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailDto> GetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProductDetailDto
            {
                Id = item.Id,
                Name = item.Name,
                Brand = item.Brand,
                Description = item.Description,
                Price = item.Price,
                ImageUrl = item.ImageUrl,
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                CreatedAt = item.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        var groups = await _db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == id && !review.IsHidden)
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var distribution = new StarDistributionDto();
        foreach (var group in groups)
        {
            switch (group.Rating)
            {
                case 1: distribution.Star1 = group.Count; break;
                case 2: distribution.Star2 = group.Count; break;
                case 3: distribution.Star3 = group.Count; break;
                case 4: distribution.Star4 = group.Count; break;
                case 5: distribution.Star5 = group.Count; break;
            }
        }

        var total = groups.Sum(group => group.Count);
        product.TotalReviews = total;
        product.AvgRating = total == 0
            ? 0
            : Math.Round(groups.Sum(group => group.Rating * (double)group.Count) / total, 2);
        product.StarDistribution = distribution;
        return product;
    }

    public async Task<PagedResult<ReviewDto>> GetReviewsAsync(int productId, ReviewFilterQuery query, CancellationToken cancellationToken)
    {
        var exists = await _db.Products.AnyAsync(product => product.Id == productId, cancellationToken);
        if (!exists)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        if (query.Star is < 1 or > 5)
            throw new AppException(StatusCodes.Status400BadRequest, "Số sao lọc phải từ 1 đến 5.");

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        IQueryable<Review> reviews = _db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == productId && !review.IsHidden);

        if (query.Star.HasValue)
            reviews = reviews.Where(review => review.Rating == query.Star.Value);

        if (query.HasImages == true)
            reviews = reviews.Where(review => review.Images.Any());
        else if (query.HasImages == false)
            reviews = reviews.Where(review => !review.Images.Any());

        reviews = (query.Sort ?? "newest").Trim().ToLowerInvariant() switch
        {
            "rating" => reviews.OrderByDescending(review => review.Rating).ThenByDescending(review => review.CreatedAt),
            _ => reviews.OrderByDescending(review => review.CreatedAt)
        };

        var total = await reviews.CountAsync(cancellationToken);
        var items = await ReviewQueries.MapAsync(
            reviews.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken);

        return new PagedResult<ReviewDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = items
        };
    }

    public async Task<FlawExtractionDto> GetFlawsAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(item => item.Id == productId)
            .Select(item => new { item.Id, item.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        var flaws = await _db.Reviews.AsNoTracking()
            .Where(review => review.ProductId == productId && !review.IsHidden && review.Rating <= 2)
            .OrderBy(review => review.Rating)
            .ThenByDescending(review => review.CreatedAt)
            .Select(review => new FlawItemDto
            {
                ReviewId = review.Id,
                Rating = review.Rating,
                Cons = review.Cons,
                CreatedAt = review.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new FlawExtractionDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            FlawReviewCount = flaws.Count,
            Flaws = flaws
        };
    }
}
