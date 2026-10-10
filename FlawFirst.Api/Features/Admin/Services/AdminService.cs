using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Admin.Services;

public interface IAdminService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<CategoryDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpsertCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken);
    Task<AdminProductDto> CreateProductAsync(UpsertProductRequest request, CancellationToken cancellationToken);
    Task<AdminProductDto> UpdateProductAsync(int id, UpsertProductRequest request, CancellationToken cancellationToken);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<ReportDto>> GetReportsAsync(ReportListQuery query, CancellationToken cancellationToken);
    Task<ModerateResultDto> ModerateAsync(int reportId, int adminId, string action, CancellationToken cancellationToken);
    Task<PagedResult<UserProfileDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<UserProfileDto> SetLockAsync(int userId, int adminId, bool isLocked, CancellationToken cancellationToken);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
        => await MapCategories().ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureCategoryNameAvailable(name, null, cancellationToken);
        var category = new Category
        {
            Name = name,
            Description = Clean(request.Description),
            CreatedAt = DateTime.UtcNow
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapCategories().FirstAsync(item => item.Id == category.Id, cancellationToken);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, UpsertCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy ngành hàng.");

        var name = request.Name.Trim();
        await EnsureCategoryNameAvailable(name, id, cancellationToken);
        category.Name = name;
        category.Description = Clean(request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapCategories().FirstAsync(item => item.Id == category.Id, cancellationToken);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy ngành hàng.");

        var hasProducts = await _db.Products.AnyAsync(product => product.CategoryId == id, cancellationToken);
        if (hasProducts)
            throw new AppException(StatusCodes.Status409Conflict, "Không thể xóa ngành hàng đang có sản phẩm.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminProductDto> CreateProductAsync(UpsertProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureCategoryExists(request.CategoryId, cancellationToken);
        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Description = Clean(request.Description),
            Price = request.Price,
            ImageUrl = Clean(request.ImageUrl),
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapProductAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductDto> UpdateProductAsync(int id, UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        await EnsureCategoryExists(request.CategoryId, cancellationToken);
        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Description = Clean(request.Description);
        product.Price = request.Price;
        product.ImageUrl = Clean(request.ImageUrl);
        product.CategoryId = request.CategoryId;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapProductAsync(product.Id, cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.");

        var hasReviews = await _db.Reviews.AnyAsync(review => review.ProductId == id, cancellationToken);
        if (hasReviews)
            throw new AppException(StatusCodes.Status409Conflict, "Không thể xóa sản phẩm đã có bài review.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<ReportDto>> GetReportsAsync(ReportListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, 20);
        IQueryable<ReviewReport> reports = _db.ReviewReports.AsNoTracking();
        var status = string.IsNullOrWhiteSpace(query.Status) ? ReportStatuses.Pending : query.Status.Trim();
        if (!status.Equals("All", StringComparison.OrdinalIgnoreCase))
            reports = reports.Where(report => report.Status == status);

        var total = await reports.CountAsync(cancellationToken);
        var rows = await reports
            .OrderByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(report => report.Reporter)
            .Include(report => report.Evidences)
            .Include(report => report.Review!)
                .ThenInclude(review => review.Product)
            .Include(report => report.Review!)
                .ThenInclude(review => review.User)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = rows.Select(MapReport).ToList()
        };
    }

    public async Task<ModerateResultDto> ModerateAsync(int reportId, int adminId, string action, CancellationToken cancellationToken)
    {
        var report = await _db.ReviewReports
            .Include(item => item.Review!)
                .ThenInclude(review => review.User)
            .Include(item => item.Review!)
                .ThenInclude(review => review.Product)
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy báo cáo.");

        if (report.Status != ReportStatuses.Pending)
            throw new AppException(StatusCodes.Status409Conflict, "Báo cáo này đã được xử lý.");

        var now = DateTime.UtcNow;
        switch (action)
        {
            case ModerationActions.Ignore:
                report.Status = ReportStatuses.Dismissed;
                report.Action = ModerationActions.Ignore;
                report.ResolvedAt = now;
                report.ResolvedByAdminId = adminId;
                await _db.SaveChangesAsync(cancellationToken);
                return Result(report, "Đã bỏ qua báo cáo.");

            case ModerationActions.Hide:
            {
                var review = RequireReview(report);
                review.IsHidden = true;
                await ResolvePendingFamilyAsync(review.Id, ModerationActions.Hide, adminId, now, cancellationToken);
                return Result(report, "Đã ẩn bài review và đóng các báo cáo đang chờ của bài này.");
            }

            case ModerationActions.Penalize:
            {
                var review = RequireReview(report);
                review.IsHidden = true;
                var lockedAuthor = false;
                if (review.User.Role != AppRoles.Admin)
                {
                    review.User.IsLocked = true;
                    lockedAuthor = true;
                }

                await ResolvePendingFamilyAsync(review.Id, ModerationActions.Penalize, adminId, now, cancellationToken);
                var message = lockedAuthor
                    ? "Đã ẩn bài review và khóa tài khoản tác giả."
                    : "Đã ẩn bài review. Không khóa tài khoản Admin.";
                return Result(report, message);
            }

            case ModerationActions.Delete:
            {
                var review = RequireReview(report);
                var snapshot = ReportSnapshots.Build(review);
                var related = await _db.ReviewReports.Where(item => item.ReviewId == review.Id).ToListAsync(cancellationToken);
                foreach (var item in related)
                {
                    item.ReviewSnapshot = snapshot;
                    if (item.Status == ReportStatuses.Pending)
                    {
                        item.Status = ReportStatuses.Resolved;
                        item.Action = ModerationActions.Delete;
                        item.ResolvedAt = now;
                        item.ResolvedByAdminId = adminId;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync(cancellationToken);
                report.Status = ReportStatuses.Resolved;
                report.Action = ModerationActions.Delete;
                return Result(report, "Đã xóa bài review.");
            }

            default:
                throw new AppException(StatusCodes.Status400BadRequest, "Hành động kiểm duyệt không hợp lệ.");
        }
    }

    public async Task<PagedResult<UserProfileDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, 20);
        IQueryable<User> users = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            users = users.Where(user => user.FullName.Contains(keyword) || user.Email.Contains(keyword));
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsLocked = user.IsLocked,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserProfileDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = items
        };
    }

    public async Task<UserProfileDto> SetLockAsync(int userId, int adminId, bool isLocked, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng.");

        if (user.Id == adminId && isLocked)
            throw new AppException(StatusCodes.Status400BadRequest, "Không thể tự khóa tài khoản đang đăng nhập.");

        user.IsLocked = isLocked;
        await _db.SaveChangesAsync(cancellationToken);
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task ResolvePendingFamilyAsync(int reviewId, string action, int adminId, DateTime now, CancellationToken cancellationToken)
    {
        var related = await _db.ReviewReports
            .Where(item => item.ReviewId == reviewId && item.Status == ReportStatuses.Pending)
            .ToListAsync(cancellationToken);

        foreach (var item in related)
        {
            item.Status = ReportStatuses.Resolved;
            item.Action = action;
            item.ResolvedAt = now;
            item.ResolvedByAdminId = adminId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<CategoryDto> MapCategories()
    {
        return _db.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = category.Products.Count(),
                CreatedAt = category.CreatedAt
            });
    }

    private async Task<AdminProductDto> MapProductAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Products.AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new AdminProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                CreatedAt = product.CreatedAt
            })
            .FirstAsync(cancellationToken);
    }

    private async Task EnsureCategoryNameAvailable(string name, int? exceptId, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();
        var exists = await _db.Categories.AnyAsync(
            category => category.Name.ToLower() == lowered && category.Id != exceptId,
            cancellationToken);
        if (exists)
            throw new AppException(StatusCodes.Status409Conflict, "Tên ngành hàng đã tồn tại.");
    }

    private async Task EnsureCategoryExists(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await _db.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);
        if (!exists)
            throw new AppException(StatusCodes.Status404NotFound, "Không tìm thấy ngành hàng.");
    }

    private static Review RequireReview(ReviewReport report)
    {
        return report.Review ?? throw new AppException(StatusCodes.Status409Conflict, "Bài review của báo cáo này không còn tồn tại.");
    }

    private static ModerateResultDto Result(ReviewReport report, string message)
    {
        return new ModerateResultDto
        {
            ReportId = report.Id,
            Status = report.Status,
            Action = report.Action ?? string.Empty,
            Message = message
        };
    }

    private static ReportDto MapReport(ReviewReport report)
    {
        return new ReportDto
        {
            Id = report.Id,
            ReviewId = report.ReviewId,
            ReviewSnapshot = report.ReviewSnapshot,
            ProductId = report.Review?.ProductId,
            ProductName = report.Review?.Product?.Name,
            Pros = report.Review?.Pros,
            Cons = report.Review?.Cons,
            ReviewRating = report.Review?.Rating,
            IsAnonymous = report.Review?.IsAnonymous,
            IsHidden = report.Review?.IsHidden,
            AuthorId = report.Review?.UserId,
            AuthorName = report.Review?.User?.FullName,
            AuthorEmail = report.Review?.User?.Email,
            ReporterId = report.ReporterId,
            ReporterName = report.Reporter.FullName,
            ReporterEmail = report.Reporter.Email,
            Reason = report.Reason,
            EvidenceImageUrls = report.Evidences.Select(evidence => evidence.ImageUrl).ToList(),
            Status = report.Status,
            Action = report.Action,
            CreatedAt = report.CreatedAt,
            ResolvedAt = report.ResolvedAt
        };
    }

    private static string? Clean(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
