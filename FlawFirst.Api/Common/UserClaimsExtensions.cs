using System.Security.Claims;

namespace FlawFirst.Api.Common;

public static class UserClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(raw, out var id))
            throw new AppException(StatusCodes.Status401Unauthorized, "Token không hợp lệ.");

        return id;
    }
}
