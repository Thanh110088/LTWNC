using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
    public DbSet<ReportEvidence> ReportEvidences => Set<ReportEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", table =>
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin', 'User')"));
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Brand).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => x.Price);
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews", table =>
                table.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
            entity.Property(x => x.Pros).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Cons).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.ProductId, x.IsHidden, x.Rating });
            entity.HasOne(x => x.Product)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReviewImage>(entity =>
        {
            entity.ToTable("ReviewImages");
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.Review)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasOne(x => x.Review)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReviewVote>(entity =>
        {
            entity.ToTable("ReviewVotes");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.ReviewId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Review)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.ToTable("Bookmarks");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.ReviewId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Review)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("UserFollows", table =>
                table.HasCheckConstraint("CK_UserFollows_NotSelf", "[FollowerId] <> [FollowingId]"));
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
            entity.HasOne(x => x.Follower)
                .WithMany(x => x.Following)
                .HasForeignKey(x => x.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FollowingUser)
                .WithMany(x => x.Followers)
                .HasForeignKey(x => x.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReviewReport>(entity =>
        {
            entity.ToTable("ReviewReports", table =>
                table.HasCheckConstraint(
                    "CK_ReviewReports_Status",
                    "[Status] IN ('Pending', 'Resolved', 'Dismissed')"));
            entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ReviewSnapshot).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.Property(x => x.ResolvedAt).HasColumnType("datetime2");
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.Review)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Reporter)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ResolvedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportEvidence>(entity =>
        {
            entity.ToTable("ReportEvidences");
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.Report)
                .WithMany(x => x.Evidences)
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
