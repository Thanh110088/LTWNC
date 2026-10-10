using FlawFirst.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlawFirst.Api.Features.Social.Controllers;

/// <summary>
/// MODULE 3 — Social (Thành viên 3).
/// FR10 báo cáo, FR12 bình luận, FR13 vote, FR14 bookmark và link chia sẻ, FR15 follow.
/// </summary>
[ApiController]
[Authorize]
[Route("api/social")]
public class SocialController : ControllerBase
{
    private readonly ISocialService _social;
    private readonly IFileStorageService _files;

    public SocialController(ISocialService social, IFileStorageService files)
    {
        _social = social;
        _files = files;
    }

    /// <summary>FR10. Gửi báo cáo vi phạm, trạng thái Pending, kèm lý do và ảnh bằng chứng.</summary>
    [HttpPost("reports")]
    public async Task<ActionResult<ReportDto>> Report(CreateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await _social.CreateReportAsync(User.GetUserId(), request, cancellationToken);
        return Ok(report);
    }

    /// <summary>Tải ảnh bằng chứng cho báo cáo.</summary>
    [HttpPost("uploads")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadResultDto>> Upload([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var urls = await _files.SaveImagesAsync(files, cancellationToken);
        return Ok(new UploadResultDto { Urls = urls });
    }

    /// <summary>FR12. Bình luận, có cờ ẩn danh.</summary>
    [HttpPost("reviews/{reviewId:int}/comments")]
    public async Task<ActionResult<CommentDto>> Comment(int reviewId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _social.AddCommentAsync(reviewId, User.GetUserId(), request, cancellationToken);
        return Ok(comment);
    }

    /// <summary>FR12. Danh sách bình luận. Người ẩn danh hiển thị "Người dùng ẩn danh".</summary>
    [AllowAnonymous]
    [HttpGet("reviews/{reviewId:int}/comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> Comments(int reviewId, [FromQuery] PagingQuery query, CancellationToken cancellationToken)
    {
        var comments = await _social.GetCommentsAsync(reviewId, query, cancellationToken);
        return Ok(comments);
    }

    /// <summary>FR13. Upvote hoặc Downvote. Bấm lại cùng loại sẽ gỡ vote. Đổi loại thì chuyển vote và cập nhật tổng.</summary>
    [HttpPost("reviews/{reviewId:int}/votes")]
    public async Task<ActionResult<VoteResultDto>> Vote(int reviewId, VoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _social.VoteAsync(reviewId, User.GetUserId(), request.VoteType, cancellationToken);
        return Ok(result);
    }

    /// <summary>FR14. Lưu bài review.</summary>
    [HttpPost("reviews/{reviewId:int}/bookmarks")]
    public async Task<ActionResult<BookmarkDto>> Bookmark(int reviewId, CancellationToken cancellationToken)
    {
        var bookmark = await _social.BookmarkAsync(reviewId, User.GetUserId(), BaseUrl(), cancellationToken);
        return Ok(bookmark);
    }

    /// <summary>FR14. Bỏ lưu bài review.</summary>
    [HttpDelete("reviews/{reviewId:int}/bookmarks")]
    public async Task<IActionResult> RemoveBookmark(int reviewId, CancellationToken cancellationToken)
    {
        await _social.RemoveBookmarkAsync(reviewId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    /// <summary>FR14. Kho bài đã lưu của tôi, mỗi bài kèm link chia sẻ.</summary>
    [HttpGet("bookmarks")]
    public async Task<ActionResult<PagedResult<BookmarkDto>>> Bookmarks([FromQuery] PagingQuery query, CancellationToken cancellationToken)
    {
        var bookmarks = await _social.GetBookmarksAsync(User.GetUserId(), query, BaseUrl(), cancellationToken);
        return Ok(bookmarks);
    }

    /// <summary>FR14. Sinh link chia sẻ tới bài review.</summary>
    [AllowAnonymous]
    [HttpGet("reviews/{reviewId:int}/share")]
    public async Task<ActionResult<ShareLinkDto>> Share(int reviewId, CancellationToken cancellationToken)
    {
        var link = await _social.GetShareLinkAsync(reviewId, BaseUrl(), cancellationToken);
        return Ok(link);
    }

    /// <summary>FR15. Theo dõi tác giả.</summary>
    [HttpPost("users/{userId:int}/follow")]
    public async Task<ActionResult<FollowResultDto>> Follow(int userId, CancellationToken cancellationToken)
    {
        var result = await _social.FollowAsync(User.GetUserId(), userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>FR15. Bỏ theo dõi.</summary>
    [HttpDelete("users/{userId:int}/follow")]
    public async Task<IActionResult> Unfollow(int userId, CancellationToken cancellationToken)
    {
        await _social.UnfollowAsync(User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }

    /// <summary>FR15. Danh sách tôi đang theo dõi.</summary>
    [HttpGet("following")]
    public async Task<ActionResult<IReadOnlyList<FollowedUserDto>>> Following(CancellationToken cancellationToken)
    {
        var users = await _social.GetFollowingAsync(User.GetUserId(), cancellationToken);
        return Ok(users);
    }

    /// <summary>FR15. Danh sách người theo dõi tôi.</summary>
    [HttpGet("followers")]
    public async Task<ActionResult<IReadOnlyList<FollowedUserDto>>> Followers(CancellationToken cancellationToken)
    {
        var users = await _social.GetFollowersAsync(User.GetUserId(), cancellationToken);
        return Ok(users);
    }

    /// <summary>Hồ sơ công khai của tác giả: tên, số follower, không trả email.</summary>
    [AllowAnonymous]
    [HttpGet("users/{userId:int}")]
    public async Task<ActionResult<PublicUserDto>> Profile(int userId, CancellationToken cancellationToken)
    {
        int? viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var profile = await _social.GetPublicProfileAsync(userId, viewerId, cancellationToken);
        return Ok(profile);
    }

    private string BaseUrl() => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
