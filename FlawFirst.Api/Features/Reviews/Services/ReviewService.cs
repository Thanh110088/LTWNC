using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Reviews.Services;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(int userId, CreateReviewRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ReviewDto>> GetMineAsync(int userId, PagingQuery query, CancellationToken cancellationToken);
    Task<ReviewDto> GetByIdAsync(int id, int? viewerId, bool isAdmin, CancellationToken cancellationToken);
    Task<ReviewDto> UpdateAsync(int id, int userId, UpdateReviewRequest request, CancellationToken cancellationToken);
    Task DeleteOwnedAsync(int id, int userId, CancellationToken cancellationToken);
}

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    private readonly IProfanityFilter _profanity;

    public ReviewService(AppDbContext db, IProfanityFilter profanity)
    {
        _db = db;
        _profanity = profanity;
    }

    public async Task<ReviewDto> CreateAsync(int userId, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var productExists = await _db.Products.AnyAsync(product => product.Id == request.ProductId, cancellationToken);
        if (!productExists)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        var duplicated = await _db.Reviews.AnyAsync(
            review => review.ProductId == request.ProductId && review.UserId == userId,
            cancellationToken);
        if (duplicated)
            throw new AppException(StatusCodes.Status409Conflict, "Bạn đã đánh giá sản phẩm này. Hãy sửa bài review hiện có.");

        var pros = RequireText(request.Pros, "Pros");
        var cons = RequireText(request.Cons, "Cons");
        _profanity.EnsureClean("Pros", pros);
        _profanity.EnsureClean("Cons", cons);
        var images = ImageUrlRules.Normalize(request.ImageUrls);

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = userId,
            Pros = pros,
            Cons = cons,
            Rating = request.Rating,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            Images = images.Select(url => new ReviewImage { ImageUrl = url }).ToList()
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(review.Id, cancellationToken);
    }

    public async Task<PagedResult<ReviewDto>> GetMineAsync(int userId, PagingQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var reviews = _db.Reviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .OrderByDescending(review => review.CreatedAt);

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

    public async Task<ReviewDto> GetByIdAsync(int id, int? viewerId, bool isAdmin, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.Id, item.UserId, item.IsHidden })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        if (review.IsHidden && review.UserId != viewerId && !isAdmin)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        return await GetMappedAsync(id, cancellationToken);
    }

    public async Task<ReviewDto> UpdateAsync(int id, int userId, UpdateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        if (review.UserId != userId)
            throw new AppException(StatusCodes.Status403Forbidden, "Bạn không có quyền sửa bài review này.");

        if (review.IsHidden)
            throw new AppException(StatusCodes.Status400BadRequest, "Bài review đang bị ẩn, không thể chỉnh sửa.");

        var pros = RequireText(request.Pros, "Pros");
        var cons = RequireText(request.Cons, "Cons");
        _profanity.EnsureClean("Pros", pros);
        _profanity.EnsureClean("Cons", cons);

        review.Pros = pros;
        review.Cons = cons;
        review.Rating = request.Rating;
        review.IsAnonymous = request.IsAnonymous;
        review.UpdatedAt = DateTime.UtcNow;

        if (request.ImageUrls != null)
        {
            var urls = ImageUrlRules.Normalize(request.ImageUrls);
            var existing = await _db.ReviewImages.Where(image => image.ReviewId == review.Id).ToListAsync(cancellationToken);
            _db.ReviewImages.RemoveRange(existing);
            foreach (var url in urls)
                _db.ReviewImages.Add(new ReviewImage { ReviewId = review.Id, ImageUrl = url });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(review.Id, cancellationToken);
    }

    public async Task DeleteOwnedAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        if (review.UserId != userId)
            throw new AppException(StatusCodes.Status403Forbidden, "Bạn không có quyền xóa bài review này.");

        await CloseReportsAsync(review, cancellationToken);
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CloseReportsAsync(Review review, CancellationToken cancellationToken)
    {
        var reports = await _db.ReviewReports.Where(report => report.ReviewId == review.Id).ToListAsync(cancellationToken);
        if (reports.Count == 0)
            return;

        var snapshot = ReportSnapshots.Build(review);
        var now = DateTime.UtcNow;
        foreach (var report in reports)
        {
            report.ReviewSnapshot = snapshot;
            if (report.Status == ReportStatuses.Pending)
            {
                report.Status = ReportStatuses.Resolved;
                report.Action = ModerationActions.Delete;
                report.ResolvedAt = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReviewDto> GetMappedAsync(int id, CancellationToken cancellationToken)
    {
        var items = await ReviewQueries.MapAsync(_db.Reviews.Where(review => review.Id == id), cancellationToken);
        return items.Single();
    }

    private static string RequireText(string value, string fieldName)
    {
        var text = value.Trim();
        if (text.Length < 10)
            throw new AppException(StatusCodes.Status400BadRequest, $"{fieldName} cần tối thiểu 10 ký tự.");

        return text;
    }
}
