using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Social.Services;

public interface ISocialService
{
    Task<CommentDto> AddCommentAsync(int reviewId, int userId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<PagedResult<CommentDto>> GetCommentsAsync(int reviewId, PagingQuery query, CancellationToken cancellationToken);
    Task<VoteResultDto> VoteAsync(int reviewId, int userId, string voteType, CancellationToken cancellationToken);
    Task<BookmarkDto> BookmarkAsync(int reviewId, int userId, string baseUrl, CancellationToken cancellationToken);
    Task RemoveBookmarkAsync(int reviewId, int userId, CancellationToken cancellationToken);
    Task<PagedResult<BookmarkDto>> GetBookmarksAsync(int userId, PagingQuery query, string baseUrl, CancellationToken cancellationToken);
    Task<ShareLinkDto> GetShareLinkAsync(int reviewId, string baseUrl, CancellationToken cancellationToken);
    Task<FollowResultDto> FollowAsync(int followerId, int followingId, CancellationToken cancellationToken);
    Task UnfollowAsync(int followerId, int followingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowedUserDto>> GetFollowingAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowedUserDto>> GetFollowersAsync(int userId, CancellationToken cancellationToken);
    Task<PublicUserDto> GetPublicProfileAsync(int userId, int? viewerId, CancellationToken cancellationToken);
    Task<ReportDto> CreateReportAsync(int userId, CreateReportRequest request, CancellationToken cancellationToken);
}

public class SocialService : ISocialService
{
    private readonly AppDbContext _db;

    public SocialService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommentDto> AddCommentAsync(int reviewId, int userId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var visible = await _db.Reviews.AnyAsync(review => review.Id == reviewId && !review.IsHidden, cancellationToken);
        if (!visible)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        var user = await _db.Users.AsNoTracking().FirstAsync(item => item.Id == userId, cancellationToken);
        var comment = new Comment
        {
            ReviewId = reviewId,
            UserId = userId,
            Content = request.Content.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            ReviewId = comment.ReviewId,
            Content = comment.Content,
            IsAnonymous = comment.IsAnonymous,
            AuthorName = comment.IsAnonymous ? ReviewDisplay.AnonymousName : user.FullName,
            AuthorId = comment.IsAnonymous ? null : user.Id,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<PagedResult<CommentDto>> GetCommentsAsync(int reviewId, PagingQuery query, CancellationToken cancellationToken)
    {
        var visible = await _db.Reviews.AnyAsync(review => review.Id == reviewId && !review.IsHidden, cancellationToken);
        if (!visible)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, 20);
        var comments = _db.Comments.AsNoTracking()
            .Where(comment => comment.ReviewId == reviewId)
            .OrderBy(comment => comment.CreatedAt);

        var total = await comments.CountAsync(cancellationToken);
        var rows = await comments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(comment => new
            {
                comment.Id,
                comment.ReviewId,
                comment.Content,
                comment.IsAnonymous,
                comment.UserId,
                comment.User.FullName,
                comment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = rows.Select(row => new CommentDto
            {
                Id = row.Id,
                ReviewId = row.ReviewId,
                Content = row.Content,
                IsAnonymous = row.IsAnonymous,
                AuthorName = row.IsAnonymous ? ReviewDisplay.AnonymousName : row.FullName,
                AuthorId = row.IsAnonymous ? null : row.UserId,
                CreatedAt = row.CreatedAt
            }).ToList()
        };
    }

    public async Task<VoteResultDto> VoteAsync(int reviewId, int userId, string voteType, CancellationToken cancellationToken)
    {
        var isUpvote = string.Equals(voteType, "Up", StringComparison.OrdinalIgnoreCase);
        if (!isUpvote && !string.Equals(voteType, "Down", StringComparison.OrdinalIgnoreCase))
            throw new AppException(StatusCodes.Status400BadRequest, "VoteType phải là Up hoặc Down.");

        var review = await _db.Reviews.FirstOrDefaultAsync(item => item.Id == reviewId && !item.IsHidden, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        if (review.UserId == userId)
            throw new AppException(StatusCodes.Status400BadRequest, "Không thể vote bài review của chính mình.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await _db.ReviewVotes.FirstOrDefaultAsync(
            vote => vote.ReviewId == reviewId && vote.UserId == userId,
            cancellationToken);

        if (existing == null)
        {
            _db.ReviewVotes.Add(new ReviewVote
            {
                ReviewId = reviewId,
                UserId = userId,
                IsUpvote = isUpvote,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (existing.IsUpvote == isUpvote)
        {
            _db.ReviewVotes.Remove(existing);
        }
        else
        {
            existing.IsUpvote = isUpvote;
        }

        await _db.SaveChangesAsync(cancellationToken);
        review.UpvoteCount = await _db.ReviewVotes.CountAsync(vote => vote.ReviewId == reviewId && vote.IsUpvote, cancellationToken);
        review.DownvoteCount = await _db.ReviewVotes.CountAsync(vote => vote.ReviewId == reviewId && !vote.IsUpvote, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var current = await _db.ReviewVotes.AsNoTracking()
            .Where(vote => vote.ReviewId == reviewId && vote.UserId == userId)
            .Select(vote => (bool?)vote.IsUpvote)
            .FirstOrDefaultAsync(cancellationToken);

        return new VoteResultDto
        {
            ReviewId = reviewId,
            CurrentVote = current switch
            {
                true => "Up",
                false => "Down",
                _ => null
            },
            UpvoteCount = review.UpvoteCount,
            DownvoteCount = review.DownvoteCount
        };
    }

    public async Task<BookmarkDto> BookmarkAsync(int reviewId, int userId, string baseUrl, CancellationToken cancellationToken)
    {
        await EnsureVisibleReviewAsync(reviewId, cancellationToken);
        var bookmark = await _db.Bookmarks.FirstOrDefaultAsync(
            item => item.ReviewId == reviewId && item.UserId == userId,
            cancellationToken);

        if (bookmark == null)
        {
            bookmark = new Bookmark
            {
                ReviewId = reviewId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Bookmarks.Add(bookmark);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var review = (await ReviewQueries.MapAsync(_db.Reviews.Where(item => item.Id == reviewId), cancellationToken)).Single();
        return new BookmarkDto
        {
            Id = bookmark.Id,
            ReviewId = reviewId,
            ShareUrl = BuildShareUrl(baseUrl, reviewId),
            CreatedAt = bookmark.CreatedAt,
            Review = review
        };
    }

    public async Task RemoveBookmarkAsync(int reviewId, int userId, CancellationToken cancellationToken)
    {
        var bookmark = await _db.Bookmarks.FirstOrDefaultAsync(
            item => item.ReviewId == reviewId && item.UserId == userId,
            cancellationToken);
        if (bookmark == null)
            return;

        _db.Bookmarks.Remove(bookmark);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<BookmarkDto>> GetBookmarksAsync(int userId, PagingQuery query, string baseUrl, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var bookmarks = _db.Bookmarks.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt);

        var total = await bookmarks.CountAsync(cancellationToken);
        var pageItems = await bookmarks
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new { item.Id, item.ReviewId, item.CreatedAt })
            .ToListAsync(cancellationToken);

        var reviewIds = pageItems.Select(item => item.ReviewId).ToList();
        var reviews = await ReviewQueries.MapAsync(_db.Reviews.Where(review => reviewIds.Contains(review.Id)), cancellationToken);
        var byId = reviews.ToDictionary(review => review.Id);

        return new PagedResult<BookmarkDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = pageItems.Select(item => new BookmarkDto
            {
                Id = item.Id,
                ReviewId = item.ReviewId,
                CreatedAt = item.CreatedAt,
                ShareUrl = BuildShareUrl(baseUrl, item.ReviewId),
                Review = byId[item.ReviewId]
            }).ToList()
        };
    }

    public async Task<ShareLinkDto> GetShareLinkAsync(int reviewId, string baseUrl, CancellationToken cancellationToken)
    {
        await EnsureVisibleReviewAsync(reviewId, cancellationToken);
        return new ShareLinkDto
        {
            ReviewId = reviewId,
            ShareUrl = BuildShareUrl(baseUrl, reviewId)
        };
    }

    public async Task<FollowResultDto> FollowAsync(int followerId, int followingId, CancellationToken cancellationToken)
    {
        if (followerId == followingId)
            throw new AppException(StatusCodes.Status400BadRequest, "Không thể tự theo dõi chính mình.");

        var target = await _db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == followingId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng.");

        var existing = await _db.UserFollows.AnyAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == followingId,
            cancellationToken);
        if (!existing)
        {
            _db.UserFollows.Add(new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new FollowResultDto
        {
            FollowerId = followerId,
            FollowingId = followingId,
            IsFollowing = true,
            FollowingName = target.FullName
        };
    }

    public async Task UnfollowAsync(int followerId, int followingId, CancellationToken cancellationToken)
    {
        var existing = await _db.UserFollows.FirstOrDefaultAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == followingId,
            cancellationToken);
        if (existing == null)
            return;

        _db.UserFollows.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowedUserDto>> GetFollowingAsync(int userId, CancellationToken cancellationToken)
    {
        return await _db.UserFollows.AsNoTracking()
            .Where(follow => follow.FollowerId == userId)
            .OrderByDescending(follow => follow.CreatedAt)
            .Select(follow => new FollowedUserDto
            {
                UserId = follow.FollowingId,
                FullName = follow.FollowingUser.FullName,
                FollowedAt = follow.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowedUserDto>> GetFollowersAsync(int userId, CancellationToken cancellationToken)
    {
        return await _db.UserFollows.AsNoTracking()
            .Where(follow => follow.FollowingId == userId)
            .OrderByDescending(follow => follow.CreatedAt)
            .Select(follow => new FollowedUserDto
            {
                UserId = follow.FollowerId,
                FullName = follow.Follower.FullName,
                FollowedAt = follow.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicUserDto> GetPublicProfileAsync(int userId, int? viewerId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng.");

        var followerCount = await _db.UserFollows.CountAsync(follow => follow.FollowingId == userId, cancellationToken);
        var followingCount = await _db.UserFollows.CountAsync(follow => follow.FollowerId == userId, cancellationToken);
        bool? isFollowing = null;
        if (viewerId.HasValue)
        {
            isFollowing = viewerId.Value != userId && await _db.UserFollows.AnyAsync(
                follow => follow.FollowerId == viewerId.Value && follow.FollowingId == userId,
                cancellationToken);
        }

        return new PublicUserDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            IsFollowing = isFollowing
        };
    }

    public async Task<ReportDto> CreateReportAsync(int userId, CreateReportRequest request, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == request.ReviewId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");

        if (review.UserId == userId)
            throw new AppException(StatusCodes.Status400BadRequest, "Không thể báo cáo bài review của chính mình.");

        var duplicated = await _db.ReviewReports.AnyAsync(
            report => report.ReviewId == review.Id && report.ReporterId == userId && report.Status == ReportStatuses.Pending,
            cancellationToken);
        if (duplicated)
            throw new AppException(StatusCodes.Status409Conflict, "Bạn đã gửi báo cáo cho bài viết này và đang chờ xử lý.");

        var evidence = ImageUrlRules.Normalize(request.EvidenceImageUrls);
        var report = new ReviewReport
        {
            ReviewId = review.Id,
            ReporterId = userId,
            Reason = request.Reason.Trim(),
            ReviewSnapshot = ReportSnapshots.Build(review),
            Status = ReportStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            Evidences = evidence.Select(url => new ReportEvidence { ImageUrl = url }).ToList()
        };

        _db.ReviewReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        var reporter = await _db.Users.AsNoTracking().FirstAsync(user => user.Id == userId, cancellationToken);
        var author = await _db.Users.AsNoTracking().FirstAsync(user => user.Id == review.UserId, cancellationToken);
        return new ReportDto
        {
            Id = report.Id,
            ReviewId = review.Id,
            ReviewSnapshot = report.ReviewSnapshot,
            ProductId = review.ProductId,
            ProductName = review.Product.Name,
            Pros = review.Pros,
            Cons = review.Cons,
            ReviewRating = review.Rating,
            IsAnonymous = review.IsAnonymous,
            IsHidden = review.IsHidden,
            AuthorId = author.Id,
            AuthorName = author.FullName,
            AuthorEmail = author.Email,
            ReporterId = reporter.Id,
            ReporterName = reporter.FullName,
            ReporterEmail = reporter.Email,
            Reason = report.Reason,
            EvidenceImageUrls = evidence,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };
    }

    private async Task EnsureVisibleReviewAsync(int reviewId, CancellationToken cancellationToken)
    {
        var exists = await _db.Reviews.AnyAsync(review => review.Id == reviewId && !review.IsHidden, cancellationToken);
        if (!exists)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy bài review.");
    }

    private static string BuildShareUrl(string baseUrl, int reviewId)
        => $"{baseUrl.TrimEnd('/')}/api/reviews/{reviewId}";
}
