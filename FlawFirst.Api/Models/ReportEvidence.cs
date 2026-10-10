namespace FlawFirst.Api.Entities;

public class ReportEvidence
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public ReviewReport Report { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
}
