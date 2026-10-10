using System.Text.RegularExpressions;
using FlawFirst.Api.Common;

namespace FlawFirst.Api.Features.Reviews.Services;

public interface IProfanityFilter
{
    void EnsureClean(string fieldName, string content);
}

public sealed class ProfanityFilter : IProfanityFilter
{
    private static readonly string[] Phrases =
    [
        "dit me", "địt mẹ", "du ma", "đụ má", "đụ mẹ", "con di", "con đĩ", "cho chet", "chó chết"
    ];

    private static readonly HashSet<string> Words = new(StringComparer.Ordinal)
    {
        "fuck", "shit", "bitch", "asshole", "bastard", "dick", "cunt", "porn",
        "dit", "địt", "đụ", "lồn", "buồi", "cặc",
        "đm", "dcm", "clgt", "dmm", "vcl", "đĩ"
    };

    public void EnsureClean(string fieldName, string content)
    {
        if (ContainsProfanity(content))
        {
            throw new AppException(
                StatusCodes.Status400BadRequest,
                $"{fieldName} chứa từ ngữ không phù hợp. Vui lòng chỉnh sửa trước khi đăng.");
        }
    }

    private static bool ContainsProfanity(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var lower = content.ToLowerInvariant();
        if (HasBannedToken(lower))
            return true;

        var collapsed = Regex.Replace(lower, @"[.*_\-]+", string.Empty);
        return HasBannedToken(collapsed);
    }

    private static bool HasBannedToken(string value)
    {
        var spaced = Regex.Replace(value, @"[^\p{L}\p{Nd}]+", " ");
        spaced = Regex.Replace(spaced, @"\s+", " ").Trim();

        foreach (var phrase in Phrases)
        {
            if (spaced.Contains(phrase, StringComparison.Ordinal))
                return true;
        }

        foreach (var token in spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Words.Contains(token))
                return true;
        }

        return false;
    }
}
