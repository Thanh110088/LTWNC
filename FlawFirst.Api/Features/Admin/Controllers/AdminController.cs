using FlawFirst.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlawFirst.Api.Features.Admin.Controllers;

/// <summary>
/// MODULE 1 FR17 quản lý ngành hàng, MODULE 3 FR11 kiểm duyệt và FR18 khóa tài khoản.
/// </summary>
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    /// <summary>FR17. Danh sách ngành hàng.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _admin.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>FR17. Tạo ngành hàng.</summary>
    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _admin.CreateCategoryAsync(request, cancellationToken);
        return Ok(category);
    }

    /// <summary>FR17. Sửa ngành hàng.</summary>
    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _admin.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(category);
    }

    /// <summary>FR17. Xóa ngành hàng khi không còn sản phẩm.</summary>
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        await _admin.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Thêm sản phẩm để catalog có dữ liệu mới.</summary>
    [HttpPost("products")]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _admin.CreateProductAsync(request, cancellationToken);
        return Ok(product);
    }

    /// <summary>Sửa sản phẩm.</summary>
    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(int id, UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _admin.UpdateProductAsync(id, request, cancellationToken);
        return Ok(product);
    }

    /// <summary>Xóa sản phẩm khi chưa có review.</summary>
    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await _admin.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>FR11. Hàng đợi báo cáo. Mặc định Status = Pending.</summary>
    [HttpGet("reports")]
    public async Task<ActionResult<PagedResult<ReportDto>>> Reports([FromQuery] ReportListQuery query, CancellationToken cancellationToken)
    {
        var reports = await _admin.GetReportsAsync(query, cancellationToken);
        return Ok(reports);
    }

    /// <summary>FR11. Hide ẩn bài, Delete xóa bài, Penalize ẩn bài và khóa tác giả, Ignore bỏ qua.</summary>
    [HttpPost("reports/{id:int}/moderate")]
    public async Task<ActionResult<ModerateResultDto>> Moderate(int id, ModerateReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _admin.ModerateAsync(id, User.GetUserId(), request.Action, cancellationToken);
        return Ok(result);
    }

    /// <summary>FR18. Danh sách người dùng.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserProfileDto>>> Users([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        var users = await _admin.GetUsersAsync(query, cancellationToken);
        return Ok(users);
    }

    /// <summary>FR18. Khóa hoặc mở khóa tài khoản qua IsLocked.</summary>
    [HttpPatch("users/{id:int}/lock")]
    public async Task<ActionResult<UserProfileDto>> SetLock(int id, SetLockRequest request, CancellationToken cancellationToken)
    {
        var user = await _admin.SetLockAsync(id, User.GetUserId(), request.IsLocked, cancellationToken);
        return Ok(user);
    }
}
