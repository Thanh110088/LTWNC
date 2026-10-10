using FlawFirst.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlawFirst.Api.Features.Catalog.Controllers;

/// <summary>
/// MODULE 1 — Catalog &amp; Discovery (Thành viên 1).
/// FR01 tìm kiếm, FR02 lọc/sắp xếp, FR03 thống kê, FR04 lọc review, FR05 flaw-first.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductCatalogService _catalog;
    private readonly IAdminService _admin;

    public ProductsController(IProductCatalogService catalog, IAdminService admin)
    {
        _catalog = catalog;
        _admin = admin;
    }

    /// <summary>Thành viên đã đăng nhập thêm sản phẩm chưa có trong danh mục, rồi viết đánh giá.</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AdminProductDto>> Create(UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _admin.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>FR01 + FR02. Tìm theo tên, thương hiệu, ngành hàng. Lọc ngành hàng và khoảng giá. Sắp xếp newest | rating | mostReviewed.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> Search([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        var result = await _catalog.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Danh sách ngành hàng để màn hình lọc dùng. CRUD admin nằm ở /api/admin/categories (FR17).</summary>
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken cancellationToken)
    {
        var categories = await _catalog.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>FR03. Chi tiết sản phẩm kèm AvgRating, StarDistribution và TotalReviews.</summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _catalog.GetDetailAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>FR04. Lọc review theo số sao, bài có ảnh, hoặc bài mới nhất.</summary>
    [AllowAnonymous]
    [HttpGet("{id:int}/reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> Reviews(int id, [FromQuery] ReviewFilterQuery query, CancellationToken cancellationToken)
    {
        var reviews = await _catalog.GetReviewsAsync(id, query, cancellationToken);
        return Ok(reviews);
    }

    /// <summary>FR05. Lọc review 1-2 sao và trích danh sách Cons / hạn chế thực tế.</summary>
    [AllowAnonymous]
    [HttpGet("{id:int}/flaws")]
    public async Task<ActionResult<FlawExtractionDto>> Flaws(int id, CancellationToken cancellationToken)
    {
        var flaws = await _catalog.GetFlawsAsync(id, cancellationToken);
        return Ok(flaws);
    }
}
