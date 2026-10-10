using FlawFirst.Api.Common;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Reviews.Services;

internal sealed class ReviewProjection
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Rating { get; set; }
    public string Pros { get; set; } = string.Empty;
    public string Cons { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsHidden { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class ReviewQueries
{
    public static async Task<List<ReviewDto>> MapAsync(IQueryable<Review> query, CancellationToken cancellationToken)
    {
        var rows = await query.AsNoTracking()
            .Select(review => new ReviewProjection
            {
                Id = review.Id,
                ProductId = review.ProductId,
                ProductName = review.Product.Name,
                Rating = review.Rating,
                Pros = review.Pros,
                Cons = review.Cons,
                IsAnonymous = review.IsAnonymous,
                AuthorFullName = review.User.FullName,
                UserId = review.UserId,
                UpvoteCount = review.UpvoteCount,
                DownvoteCount = review.DownvoteCount,
                CommentCount = review.Comments.Count(),
                IsHidden = review.IsHidden,
                ImageUrls = review.Images
                    .OrderBy(image => image.Id)
                    .Select(image => image.ImageUrl)
                    .ToList(),
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new ReviewDto
        {
            Id = row.Id,
            ProductId = row.ProductId,
            ProductName = row.ProductName,
            Rating = row.Rating,
            Pros = row.Pros,
            Cons = row.Cons,
            IsAnonymous = row.IsAnonymous,
            AuthorName = row.IsAnonymous ? ReviewDisplay.AnonymousName : row.AuthorFullName,
            AuthorId = row.IsAnonymous ? null : row.UserId,
            UpvoteCount = row.UpvoteCount,
            DownvoteCount = row.DownvoteCount,
            CommentCount = row.CommentCount,
            IsHidden = row.IsHidden,
            ImageUrls = row.ImageUrls,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        }).ToList();
    }
}
