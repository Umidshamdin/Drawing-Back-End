using Drawing.Application.Features.Auth.DTOs;

namespace Drawing.Application.Abstractions.Identity;
public interface IAuthService
{
    Task<RegistrationResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ResendConfirmationAsync(string email);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<UserInfoDto> UpdateProfileAsync(Guid userId, UpdateAccountRequest request);
    Task<UserInfoDto?> GetCurrentUserAsync(Guid userId);
}
