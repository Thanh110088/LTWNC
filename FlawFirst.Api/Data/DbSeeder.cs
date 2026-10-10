using FlawFirst.Api.Common;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var adminHash = BCrypt.Net.BCrypt.HashPassword(SeedAccounts.AdminPassword);
        var userHash = BCrypt.Net.BCrypt.HashPassword(SeedAccounts.DemoUserPassword);

        var admin = new User
        {
            FullName = "Nguyễn Quản Trị",
            Email = SeedAccounts.AdminEmail,
            PasswordHash = adminHash,
            Role = AppRoles.Admin,
            CreatedAt = now.AddDays(-40)
        };
        var an = new User
        {
            FullName = "Nguyễn An",
            Email = SeedAccounts.DemoUserEmail,
            PasswordHash = userHash,
            Role = AppRoles.User,
            CreatedAt = now.AddDays(-30)
        };
        var binh = new User
        {
            FullName = "Trần Bình",
            Email = "binh.tran@flawfirst.local",
            PasswordHash = userHash,
            Role = AppRoles.User,
            CreatedAt = now.AddDays(-20)
        };
        var chi = new User
        {
            FullName = "Lê Chi",
            Email = "chi.le@flawfirst.local",
            PasswordHash = userHash,
            Role = AppRoles.User,
            CreatedAt = now.AddDays(-15)
        };

        var phone = new Category { Name = "Điện thoại", Description = "Điện thoại thông minh", CreatedAt = now.AddDays(-40) };
        var laptop = new Category { Name = "Laptop", Description = "Máy tính xách tay", CreatedAt = now.AddDays(-40) };
        var beauty = new Category { Name = "Mỹ phẩm", Description = "Chăm sóc da và trang điểm", CreatedAt = now.AddDays(-40) };
        var home = new Category { Name = "Gia dụng", Description = "Thiết bị gia đình", CreatedAt = now.AddDays(-40) };

        var iphone = Product(phone, "iPhone 15", "Apple", 22990000m, "Màn hình 6.1 inch, chip A16, camera 48MP.", now.AddDays(-30));
        var galaxy = Product(phone, "Samsung Galaxy S24", "Samsung", 18990000m, "Màn hình Dynamic AMOLED, pin dùng cả ngày.", now.AddDays(-20));
        var redmi = Product(phone, "Xiaomi Redmi Note 13", "Xiaomi", 5490000m, "Máy tầm trung, pin lớn cho nhu cầu học tập.", now.AddDays(-10));
        var macbook = Product(laptop, "MacBook Air M3", "Apple", 27990000m, "Máy mỏng nhẹ, pin học và làm việc cả ngày.", now.AddDays(-25));
        var dell = Product(laptop, "Dell XPS 13", "Dell", 32990000m, "Ultrabook màn hình viền mỏng.", now.AddDays(-18));
        var asus = Product(laptop, "ASUS TUF Gaming F15", "ASUS", 21490000m, "Laptop chơi game tầm trung, tản nhiệt lớn.", now.AddDays(-8));
        var sunscreen = Product(beauty, "La Roche-Posay Anthelios SPF50", "La Roche-Posay", 445000m, "Kem chống nắng cho da dầu.", now.AddDays(-12));
        var cerave = Product(beauty, "CeraVe Sữa rửa mặt", "CeraVe", 320000m, "Sữa rửa mặt dịu, có ceramide.", now.AddDays(-6));
        var airFryer = Product(home, "Nồi chiên không dầu Philips HD9200", "Philips", 2490000m, "Nồi chiên dung tích vừa cho gia đình nhỏ.", now.AddDays(-15));
        var sharp = Product(home, "Máy lọc không khí Sharp FP-J40", "Sharp", 4990000m, "Lọc bụi mịn cho phòng ngủ.", now.AddDays(-2));

        var anIphone = Review(iphone, an, 5, false, now.AddDays(-9),
            "Màn hình sáng, camera chân dung đẹp và cầm máy chắc tay.",
            "Giá còn cao so với cấu hình năm ngoái, hộp không kèm củ sạc.");
        var binhIphone = Review(iphone, binh, 2, false, now.AddDays(-4),
            "Thiết kế vẫn sang, hệ điều hành mượt trong những ngày đầu.",
            "Pin tụt rõ sau khoảng hai tháng, máy nóng khi quay video và giá thay pin chính hãng khó chấp nhận.",
            "/uploads/samples/iphone-heat.jpg");
        var chiIphone = Review(iphone, chi, 4, true, now.AddDays(-2),
            "Face ID nhạy, ứng dụng học tập chạy ổn định.",
            "Chỉ một camera sau nên thiếu chiều sâu khi zoom xa.");

        var anGalaxy = Review(galaxy, an, 1, false, now.AddDays(-7),
            "Màn hình ngoài đẹp và hãng cam kết cập nhật lâu.",
            "Viền màn hình ám màu, loa ngoài nhỏ và cảm biến vân tay chậm khi tay ẩm.");
        var binhGalaxy = Review(galaxy, binh, 3, false, now.AddDays(-3),
            "Pin dùng được cả ngày, màn hình vẫn đọc được ngoài trời.",
            "Phần mềm còn nhiều ứng dụng cài sẵn, máy hơi dày khi bỏ túi.");

        var chiRedmi = Review(redmi, chi, 2, false, now.AddDays(-1),
            "Giá dễ mua, pin đủ cho một ngày lên lớp.",
            "Màn hình giật khi chơi game, loa mỏng và vỏ nhựa dễ trầy sau vài tuần.",
            "/uploads/samples/redmi-scratch.jpg");

        var anMac = Review(macbook, an, 5, false, now.AddDays(-6),
            "Máy nhẹ, pin học cả ngày và bàn phím gõ êm.",
            "Ít cổng kết nối, muốn thêm màn hình phải mua hub.");
        var binhMac = Review(macbook, binh, 4, true, now.AddDays(-5),
            "Màn hình đẹp khi làm đồ án và xuất file.",
            "Giá phụ kiện chính hãng cao, không nâng cấp RAM sau khi mua.");

        var chiDell = Review(dell, chi, 1, true, now.AddHours(-20),
            "Vỏ nhôm nhìn ổn lúc mới khui hộp.",
            "Quạt kêu to khi mở nhiều tab, pin thực tế chỉ khoảng 4 giờ và trackpad kém mượt.");

        var anSunscreen = Review(sunscreen, an, 2, false, now.AddHours(-12),
            "Chất kem dễ tán, không để lại vệt trắng quá đậm.",
            "Gây cay mắt khi đổ mồ hôi, lớp finish bóng dầu sau ba giờ và giá cao so với dung tích.",
            "/uploads/samples/sunscreen-finish.jpg");
        var binhSunscreen = Review(sunscreen, binh, 5, false, now.AddHours(-8),
            "Nâng tông nhẹ, không bết khi dùng với da dầu.",
            "Vòi nhựa hơi cứng, cần lắc đều trước khi dùng.");

        var chiFryer = Review(airFryer, chi, 3, false, now.AddHours(-6),
            "Nấu khoai và gà nhanh, khay dễ vệ sinh.",
            "Cửa kính mờ dầu sau vài lần nấu, tiếng quạt ồn vào buổi tối.");
        var anFryer = Review(airFryer, an, 4, false, now.AddHours(-3),
            "Thức ăn giòn đều và hẹn giờ rõ ràng.",
            "Khay hơi nông khi nấu cho cả nhà.");

        db.Users.AddRange(admin, an, binh, chi);
        db.Categories.AddRange(phone, laptop, beauty, home);
        db.Products.AddRange(iphone, galaxy, redmi, macbook, dell, asus, cerave, airFryer, sharp);
        db.Reviews.AddRange(
            anIphone, binhIphone, chiIphone,
            anGalaxy, binhGalaxy,
            chiRedmi,
            anMac, binhMac,
            chiDell,
            anSunscreen, binhSunscreen,
            chiFryer, anFryer);

        db.Comments.AddRange(
            new Comment
            {
                Review = binhIphone,
                User = an,
                Content = "Mình dùng cùng lô thì pin vẫn ổn, có thể do thói quen sạc qua đêm.",
                IsAnonymous = false,
                CreatedAt = now.AddHours(-5)
            },
            new Comment
            {
                Review = binhIphone,
                User = chi,
                Content = "Máy mình cũng nóng khi quay video ngoài trời.",
                IsAnonymous = true,
                CreatedAt = now.AddHours(-2)
            });

        db.ReviewVotes.AddRange(
            new ReviewVote { Review = binhIphone, User = an, IsUpvote = true, CreatedAt = now.AddHours(-4) },
            new ReviewVote { Review = anIphone, User = chi, IsUpvote = false, CreatedAt = now.AddHours(-3) },
            new ReviewVote { Review = anMac, User = binh, IsUpvote = true, CreatedAt = now.AddHours(-2) });

        db.Bookmarks.Add(new Bookmark
        {
            Review = chiRedmi,
            User = an,
            CreatedAt = now.AddHours(-1)
        });

        db.UserFollows.AddRange(
            new UserFollow { Follower = an, FollowingUser = binh, CreatedAt = now.AddDays(-2) },
            new UserFollow { Follower = chi, FollowingUser = an, CreatedAt = now.AddDays(-1) });

        db.ReviewReports.Add(new ReviewReport
        {
            Review = anGalaxy,
            Reporter = binh,
            Reason = "Phần hạn chế thiếu ảnh minh chứng và dễ bị hiểu là bôi nhọ thương hiệu.",
            ReviewSnapshot = ReportSnapshots.Build(anGalaxy),
            Status = ReportStatuses.Pending,
            CreatedAt = now.AddHours(-1),
            Evidences =
            [
                new ReportEvidence { ImageUrl = "/uploads/samples/report-galaxy.jpg" }
            ]
        });

        await db.SaveChangesAsync();

        var voteStats = await db.ReviewVotes
            .GroupBy(vote => vote.ReviewId)
            .Select(group => new
            {
                ReviewId = group.Key,
                Up = group.Count(vote => vote.IsUpvote),
                Down = group.Count(vote => !vote.IsUpvote)
            })
            .ToListAsync();

        var reviewedIds = voteStats.Select(stat => stat.ReviewId).ToList();
        var votedReviews = await db.Reviews.Where(review => reviewedIds.Contains(review.Id)).ToListAsync();
        foreach (var review in votedReviews)
        {
            var stat = voteStats.First(item => item.ReviewId == review.Id);
            review.UpvoteCount = stat.Up;
            review.DownvoteCount = stat.Down;
        }

        await db.SaveChangesAsync();
    }

    private static Product Product(Category category, string name, string brand, decimal price, string description, DateTime createdAt)
    {
        return new Product
        {
            Category = category,
            Name = name,
            Brand = brand,
            Price = price,
            Description = description,
            CreatedAt = createdAt
        };
    }

    private static Review Review(
        Product product,
        User author,
        int rating,
        bool anonymous,
        DateTime createdAt,
        string pros,
        string cons,
        params string[] images)
    {
        return new Review
        {
            Product = product,
            User = author,
            Rating = rating,
            Pros = pros,
            Cons = cons,
            IsAnonymous = anonymous,
            CreatedAt = createdAt,
            Images = images.Select(url => new ReviewImage { ImageUrl = url }).ToList()
        };
    }
}
