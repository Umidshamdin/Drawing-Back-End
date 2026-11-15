using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Common.Constants;
using Drawing.Application.Features.Auth.DTOs;
using Drawing.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Drawing.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DrawingDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailDelivery _email;

    public AuthService(UserManager<ApplicationUser> userManager, DrawingDbContext db, IConfiguration configuration, IEmailDelivery email)
    {
        _userManager = userManager;
        _db = db;
        _configuration = configuration;
        _email = email;
    }

    private static string Encode(string value) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
        }
        catch
        {
            throw new ArgumentException("Token formatı düzgün deyil.");
        }
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded) return;
        throw new ArgumentException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    private string FrontendUrl(string page, Guid userId, string token)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("Frontend:BaseUrl konfiqurasiyası yoxdur.");

        return $"{baseUrl}/{page}?userId={userId}&token={Uri.EscapeDataString(Encode(token))}";
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = FrontendUrl("verify-email", user.Id, token);

        await _email.SendAsync(user.Email!, "Email təsdiqi", $"Hesabınızı təsdiqləmək üçün keçid:\n{link}");
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

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = _configuration["Jwt:Key"];
        var minutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes");

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT konfiqurasiyası natamamdır.");

        if (Encoding.UTF8.GetByteCount(key) < 32 || minutes <= 0)
            throw new InvalidOperationException("JWT key və ya müddət parametri düzgün deyil.");

        var userInfo = await MapUserAsync(user);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(minutes);
        var refreshExpiresAt = now.AddDays(30);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("stamp", user.SecurityStamp ?? string.Empty)
        };

        foreach (var role in userInfo.Roles) claims.Add(new Claim("role", role));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: expiresAt, signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

        _db.RefreshSessions.Add(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt
        });

        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            RefreshExpiresAt = refreshExpiresAt,
            TokenType = "Bearer",
            User = userInfo
        };
    }

    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Ad, email və şifrə tələb olunur.");

        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            throw new ArgumentException("Bu email ilə hesab artıq mövcuddur.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        EnsureSucceeded(await _userManager.CreateAsync(user, request.Password));

        var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.User);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            EnsureSucceeded(roleResult);
        }

        await SendConfirmationAsync(user);

        return new RegistrationResponse("Qeydiyyat yaradıldı. Email təsdiq keçidini yoxlayın.");
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return null;

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || user.IsBlocked || !user.EmailConfirmed) return null;
        if (await _userManager.IsLockedOutAsync(user)) return null;

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var tokenHash = HashToken(refreshToken);
        var session = await _db.RefreshSessions.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTime.UtcNow) return null;

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());

        if (user is null || user.IsBlocked || !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user)) return null;

        session.RevokedAt = DateTime.UtcNow;

        var response = await IssueTokensAsync(user);

        await transaction.CommitAsync();

        return response;
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        var now = DateTime.UtcNow;

        await _db.RefreshSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.RevokedAt, now));

        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new ArgumentException("Təsdiq məlumatları yanlışdır.");

        if (user.EmailConfirmed) return;

        EnsureSucceeded(await _userManager.ConfirmEmailAsync(user, Decode(request.Token)));
    }

    public async Task ResendConfirmationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null || user.EmailConfirmed || user.IsBlocked) return;

        await SendConfirmationAsync(user);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null || user.IsBlocked || !user.EmailConfirmed) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = FrontendUrl("reset-password", user.Id, token);

        await _email.SendAsync(user.Email!, "Şifrənin bərpası", $"Yeni şifrə təyin etmək üçün keçid:\n{link}");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new ArgumentException("Şifrəni bərpa etmək mümkün olmadı.");

        EnsureSucceeded(await _userManager.ResetPasswordAsync(user, Decode(request.Token), request.NewPassword));

        await LogoutAsync(user.Id);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        EnsureSucceeded(await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));

        await LogoutAsync(user.Id);
    }

    public async Task<UserInfoDto> UpdateProfileAsync(Guid userId, UpdateAccountRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        if (string.IsNullOrWhiteSpace(request.FullName)) throw new ArgumentException("Ad və soyad tələb olunur.");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        EnsureSucceeded(await _userManager.UpdateAsync(user));

        return await MapUserAsync(user);
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || user.IsBlocked || !user.EmailConfirmed) return null;

        return await MapUserAsync(user);
    }
}
