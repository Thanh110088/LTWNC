namespace FlawFirst.Api.Entities;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Pros { get; set; } = string.Empty;
    public string Cons { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsHidden { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public ICollection<ReviewReport> Reports { get; set; } = new List<ReviewReport>();
}
