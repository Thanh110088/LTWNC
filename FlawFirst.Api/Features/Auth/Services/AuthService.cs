using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlawFirst.Api.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthService(AppDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        var exists = await _db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (exists)
            throw new AppException(StatusCodes.Status409Conflict, "Email đã được đăng ký.");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = AppRoles.User,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Email đã được đăng ký.");
        }

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new AppException(StatusCodes.Status401Unauthorized, "Email hoặc mật khẩu không đúng.");

        if (user.IsLocked)
            throw new AppException(StatusCodes.Status403Forbidden, "Tài khoản đã bị khóa.");

        return ToResponse(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new AppException(StatusCodes.Status401Unauthorized, "Tài khoản không tồn tại.");

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedAt
        };
    }

    private AuthResponse ToResponse(User user)
    {
        var (token, expiresAt) = _tokens.CreateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
