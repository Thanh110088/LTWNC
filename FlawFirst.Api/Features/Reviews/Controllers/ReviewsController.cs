using FlawFirst.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlawFirst.Api.Features.Reviews.Controllers;

/// <summary>
/// MODULE 2 — Review Engine (Thành viên 2).
/// FR06 đăng review Pros/Cons, FR07 ẩn danh, FR08 quản lý bài của tôi, FR09 lọc từ ngữ.
/// </summary>
[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly IFileStorageService _files;

    public ReviewsController(IReviewService reviews, IFileStorageService files)
    {
        _reviews = reviews;
        _files = files;
    }

    /// <summary>FR06 + FR07 + FR09. Bắt buộc Pros và Cons riêng biệt, số sao 1-5, có thể đính kèm ảnh và ẩn danh.</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var created = await _reviews.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Tải ảnh minh chứng, nhận về URL để đưa vào ImageUrls. Tối đa 6 ảnh, mỗi ảnh 5MB.</summary>
    [Authorize]
    [HttpPost("uploads")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadResultDto>> Upload([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var urls = await _files.SaveImagesAsync(files, cancellationToken);
        return Ok(new UploadResultDto { Urls = urls });
    }

    /// <summary>FR08. Danh sách review của đúng người đang đăng nhập.</summary>
    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> Mine([FromQuery] PagingQuery query, CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetMineAsync(User.GetUserId(), query, cancellationToken);
        return Ok(reviews);
    }

    /// <summary>Chi tiết một bài review. Bài ẩn danh trả AuthorName = "Người dùng ẩn danh" và không có email.</summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewDto>> GetById(int id, CancellationToken cancellationToken)
    {
        int? viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var review = await _reviews.GetByIdAsync(id, viewerId, User.IsInRole(AppRoles.Admin), cancellationToken);
        return Ok(review);
    }

    /// <summary>FR08. Chỉ chủ bài, lấy từ token, mới được sửa.</summary>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewDto>> Update(int id, UpdateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _reviews.UpdateAsync(id, User.GetUserId(), request, cancellationToken);
        return Ok(review);
    }

    /// <summary>FR08. Chỉ chủ bài mới được xóa.</summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _reviews.DeleteOwnedAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
