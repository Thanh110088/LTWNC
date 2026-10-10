namespace FlawFirst.Api.Entities;

public class ReviewImage
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
}
