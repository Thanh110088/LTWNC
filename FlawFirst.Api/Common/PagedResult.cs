namespace FlawFirst.Api.Common;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}

public static class Paging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultSize = 10)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? defaultSize : Math.Min(pageSize, 50);
        return (normalizedPage, normalizedSize);
    }
}
