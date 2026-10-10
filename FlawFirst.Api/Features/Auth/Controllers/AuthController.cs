using FlawFirst.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlawFirst.Api.Features.Auth.Controllers;

/// <summary>
/// MODULE 3 — IAM. FR16: đăng ký, đăng nhập, cấp JWT chứa Role.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>FR16. Đăng ký tài khoản User, mật khẩu băm bằng BCrypt, trả JWT hạn 7 ngày.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _auth.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>FR16. Đăng nhập. Tài khoản IsLocked nhận 403 và không được cấp token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _auth.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Thông tin tài khoản lấy từ token.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
    {
        var profile = await _auth.GetProfileAsync(User.GetUserId(), cancellationToken);
        return Ok(profile);
    }
}
