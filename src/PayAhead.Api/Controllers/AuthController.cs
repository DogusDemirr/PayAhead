using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PayAhead.Application.Auth;

namespace PayAhead.Api.Controllers;

[ApiController, Route("api/auth"), AllowAnonymous, EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthService auth, PasswordResetService reset) : ControllerBase
{
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await reset.ForgotAsync(request, ct);
        return Accepted(new { message = "Adres kayıtlıysa şifre yenileme bağlantısı gönderilecektir." });
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await reset.ResetAsync(request, ct); return NoContent();
    }
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken ct)
        => StatusCode(201, await auth.RegisterAsync(request, ct));
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await auth.LoginAsync(request, ct));
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
        => Ok(await auth.RefreshAsync(request, ct));
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }
}

[ApiController, Route("api/users"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(IAuthService auth) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) => Ok(await auth.MeAsync(ct));
}
