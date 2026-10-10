namespace FlawFirst.Api.Entities;

public class ReviewReport
{
    public int Id { get; set; }
    public int? ReviewId { get; set; }
    public Review? Review { get; set; }
    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public string? ReviewSnapshot { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Action { get; set; }
    public int? ResolvedByAdminId { get; set; }
    public User? ResolvedByAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ICollection<ReportEvidence> Evidences { get; set; } = new List<ReportEvidence>();
}
