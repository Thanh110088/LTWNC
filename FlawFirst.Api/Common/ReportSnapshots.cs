using FlawFirst.Api.Entities;

namespace FlawFirst.Api.Common;

public static class ReportSnapshots
{
    public static string Build(Review review)
    {
        var name = review.Product?.Name ?? "Sản phẩm";
        var cons = review.Cons ?? string.Empty;
        if (cons.Length > 300)
            cons = cons[..300];

        var snapshot = $"{name} | {review.Rating}* | {cons}";
        return snapshot.Length <= 1000 ? snapshot : snapshot[..1000];
    }
}
