using System.ComponentModel.DataAnnotations;

namespace Drawing.Application.Features.Auth.DTOs;
public sealed record RegistrationResponse(string Message);

public sealed class ConfirmEmailRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;
}

public sealed class EmailRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class UpdateAccountRequest
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }
}
