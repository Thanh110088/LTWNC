namespace FlawFirst.Api.Features.Catalog.Models;

public class ProductQuery
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>newest | rating | mostReviewed</summary>
    public string Sort { get; set; } = "newest";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class ProductListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StarDistributionDto
{
    public int Star1 { get; set; }
    public int Star2 { get; set; }
    public int Star3 { get; set; }
    public int Star4 { get; set; }
    public int Star5 { get; set; }
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public StarDistributionDto StarDistribution { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ReviewFilterQuery
{
    public int? Star { get; set; }
    public bool? HasImages { get; set; }

    /// <summary>newest | rating</summary>
    public string Sort { get; set; } = "newest";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class FlawItemDto
{
    public int ReviewId { get; set; }
    public int Rating { get; set; }
    public string Cons { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class FlawExtractionDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int FlawReviewCount { get; set; }
    public IReadOnlyList<FlawItemDto> Flaws { get; set; } = Array.Empty<FlawItemDto>();
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
