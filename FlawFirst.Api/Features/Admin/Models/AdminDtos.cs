using System.ComponentModel.DataAnnotations;

namespace FlawFirst.Api.Features.Admin.Models;

public class UpsertCategoryRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên ngành hàng.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Tên ngành hàng phải từ 2 đến 120 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpsertProductRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập thương hiệu.")]
    [StringLength(120, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0, 999999999)]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Ngành hàng không hợp lệ.")]
    public int CategoryId { get; set; }
}

public class AdminProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ModerateReportRequest
{
    [Required(ErrorMessage = "Vui lòng chọn hành động.")]
    [RegularExpression("^(Hide|Delete|Penalize|Ignore)$", ErrorMessage = "Action phải là Hide, Delete, Penalize hoặc Ignore.")]
    public string Action { get; set; } = string.Empty;
}

public class ModerateResultDto
{
    public int ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ReportListQuery
{
    /// <summary>Pending | Resolved | Dismissed | All. Mặc định Pending.</summary>
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ReportDto
{
    public int Id { get; set; }
    public int? ReviewId { get; set; }
    public string? ReviewSnapshot { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Pros { get; set; }
    public string? Cons { get; set; }
    public int? ReviewRating { get; set; }
    public bool? IsAnonymous { get; set; }
    public bool? IsHidden { get; set; }
    public int? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public int ReporterId { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<string> EvidenceImageUrls { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = string.Empty;
    public string? Action { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class UserListQuery
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SetLockRequest
{
    public bool IsLocked { get; set; }
}
