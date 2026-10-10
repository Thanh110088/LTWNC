namespace FlawFirst.Api.Common;

public static class ImageUrlRules
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? urls, int max = 6)
    {
        var list = (urls ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count > max)
            throw new AppException(StatusCodes.Status400BadRequest, $"Chỉ được đính kèm tối đa {max} ảnh.");

        foreach (var url in list)
        {
            if (url.Length > 500 || !IsSafe(url))
            {
                throw new AppException(
                    StatusCodes.Status400BadRequest,
                    "Đường dẫn ảnh không hợp lệ. Hãy tải ảnh lên trước hoặc dùng liên kết http(s).");
            }
        }

        return list;
    }

    public static bool IsSafe(string url)
    {
        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return !url.Contains("..", StringComparison.Ordinal);

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
