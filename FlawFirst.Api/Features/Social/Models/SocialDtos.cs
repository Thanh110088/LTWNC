using System.ComponentModel.DataAnnotations;

namespace FlawFirst.Api.Features.Social.Models;

public class CreateCommentRequest
{
    [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Bình luận tối đa 2000 ký tự.")]
    public string Content { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int? AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VoteRequest
{
    [Required(ErrorMessage = "Vui lòng chọn Up hoặc Down.")]
    [RegularExpression("^(Up|Down)$", ErrorMessage = "VoteType phải là Up hoặc Down.")]
    public string VoteType { get; set; } = string.Empty;
}

public class VoteResultDto
{
    public int ReviewId { get; set; }
    public string? CurrentVote { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
}

public class CreateReportRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Bài review không hợp lệ.")]
    public int ReviewId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do báo cáo.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Lý do cần từ 10 đến 2000 ký tự.")]
    public string Reason { get; set; } = string.Empty;

    public List<string>? EvidenceImageUrls { get; set; }
}

public class BookmarkDto
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ReviewDto Review { get; set; } = new();
}

public class ShareLinkDto
{
    public int ReviewId { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
}

public class FollowResultDto
{
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
    public bool IsFollowing { get; set; }
    public string FollowingName { get; set; } = string.Empty;
}

public class FollowedUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
}

public class PublicUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool? IsFollowing { get; set; }
}
