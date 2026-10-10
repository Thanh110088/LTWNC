using System.Security.Claims;
using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Middleware;

/// <summary>
/// Development only. Nếu request chưa có JWT hợp lệ, gán danh tính seed
/// để thành viên catalog và review gọi được các API có [Authorize].
/// Header X-Mock-Role: Admin hoặc Authorization: Bearer mock-admin để vào vai quản trị.
/// </summary>
public class MockAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public MockAuthMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (!_environment.IsDevelopment() || context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsSkipped(path))
        {
            await _next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        var hasAuthorization = !string.IsNullOrWhiteSpace(authorization);
        var mockToken = authorization.StartsWith("Bearer mock", StringComparison.OrdinalIgnoreCase);
        if (hasAuthorization && !mockToken)
        {
            await _next(context);
            return;
        }

        var role = ResolveRole(context, authorization);
        var email = role == AppRoles.Admin ? SeedAccounts.AdminEmail : SeedAccounts.DemoUserEmail;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Email == email);
        if (user == null)
        {
            await _next(context);
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.Name, user.FullName)
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Mock"));
        await _next(context);
    }

    private static bool IsSkipped(string path)
    {
        return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRole(HttpContext context, string authorization)
    {
        if (authorization.Contains("mock-admin", StringComparison.OrdinalIgnoreCase))
            return AppRoles.Admin;

        var header = context.Request.Headers["X-Mock-Role"].FirstOrDefault();
        if (string.Equals(header, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return AppRoles.Admin;

        return AppRoles.User;
    }
}
