using Drawing.Application.Features.Auth.DTOs;

namespace Drawing.Application.Abstractions.Identity;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserInfoDto?> GetCurrentUserAsync(Guid userId);
}