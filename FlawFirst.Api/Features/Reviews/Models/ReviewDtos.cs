using System.ComponentModel.DataAnnotations;

namespace FlawFirst.Api.Features.Reviews.Models;

public class CreateReviewRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập điểm hài lòng (Pros).")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Pros cần từ 10 đến 4000 ký tự.")]
    public string Pros { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hạn chế thực tế (Cons).")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Cons cần từ 10 đến 4000 ký tự.")]
    public string Cons { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public int Rating { get; set; }

    public bool IsAnonymous { get; set; }

    public List<string>? ImageUrls { get; set; }
}

public class UpdateReviewRequest
{
    [Required(ErrorMessage = "Vui lòng nhập điểm hài lòng (Pros).")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Pros cần từ 10 đến 4000 ký tự.")]
    public string Pros { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hạn chế thực tế (Cons).")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Cons cần từ 10 đến 4000 ký tự.")]
    public string Cons { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public int Rating { get; set; }

    public bool IsAnonymous { get; set; }

    /// <summary>Null giữ nguyên ảnh. Mảng rỗng xóa hết ảnh.</summary>
    public List<string>? ImageUrls { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Rating { get; set; }
    public string Pros { get; set; } = string.Empty;
    public string Cons { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int? AuthorId { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsHidden { get; set; }
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UploadResultDto
{
    public IReadOnlyList<string> Urls { get; set; } = Array.Empty<string>();
}

public class PagingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
