using System.Security.Claims;
using FlawFirst.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Middleware;

public class LockedAccountMiddleware
{
    private readonly RequestDelegate _next;

    public LockedAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var raw = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(raw, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Token không hợp lệ." });
            return;
        }

        var user = await db.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.IsLocked })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Tài khoản không tồn tại." });
            return;
        }

        if (user.IsLocked)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Tài khoản đã bị khóa." });
            return;
        }

        await _next(context);
    }
}
