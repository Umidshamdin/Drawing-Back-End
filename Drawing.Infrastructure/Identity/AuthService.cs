using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Common.Constants;
using Drawing.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Drawing.Infrastructure.Identity;
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Ad və soyad tələb olunur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email tələb olunur.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Şifrə tələb olunur.");

        var email = request.Email.Trim();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
            throw new ArgumentException(
                "Bu email ilə istifadəçi artıq mövcuddur.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            UserName = email,
            Email = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new ArgumentException(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.User);

        if (!roleResult.Succeeded)
        {
            var deleteResult = await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
                throw new InvalidOperationException(
                    "Account role assignment failed and cleanup was unsuccessful.");

            throw new InvalidOperationException(
                "Account role assignment failed.");
        }

        return await GenerateAuthResponseAsync(user);
    }
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null || user.IsBlocked)
            return null;

        if (!await _userManager.CheckPasswordAsync(
            user,
            request.Password))
            return null;

        return await GenerateAuthResponseAsync(user);
    }
    public async Task<UserInfoDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null || user.IsBlocked)
            return null;

        return await MapUserAsync(user);
    }
    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var userInfo = await MapUserAsync(user);

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(issuer)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing.");
        }

        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");

        var expiryMinutes = _configuration
            .GetValue<int>("Jwt:ExpiryMinutes");

        if (expiryMinutes <= 0)
            throw new InvalidOperationException(
                "JWT expiry configuration is invalid.");

        var expiresAt = DateTime.UtcNow.AddMinutes(
            expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim("role", role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = userInfo
        };
    }
    private async Task<UserInfoDto> MapUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfoDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Roles = roles
        };
    }
}