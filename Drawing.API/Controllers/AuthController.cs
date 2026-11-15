using Drawing.API.Infrastructure;
using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Accepted(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result is null ? Unauthorized(new { Message = "Giriş məlumatları yanlışdır və ya hesab aktiv deyil." }) : Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);
        return result is null ? Unauthorized(new { Message = "Sessiya etibarsızdır." }) : Ok(result);
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request);
        return Ok(new { Message = "Email təsdiqləndi." });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequest request)
    {
        await _authService.ResendConfirmationAsync(request.Email);
        return Ok(new { Message = "Uyğun hesab mövcuddursa, təsdiq məktubu göndəriləcək." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        return Ok(new { Message = "Uyğun hesab mövcuddursa, şifrə bərpası məktubu göndəriləcək." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { Message = "Şifrə yeniləndi." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync(this.UserId());
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAccountRequest request)
    {
        return Ok(await _authService.UpdateProfileAsync(this.UserId(), request));
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(this.UserId(), request);
        return Ok(new { Message = "Şifrə dəyişdirildi. Yenidən daxil olun." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(this.UserId());
        return NoContent();
    }

    [HttpGet("admin-test")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminTest()
    {
        return Ok(new { Message = "Administrator icazəsi təsdiqləndi." });
    }
}
