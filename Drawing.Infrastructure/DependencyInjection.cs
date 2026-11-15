using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Abstractions.Persistence;
using Drawing.Application.Common.Constants;
using Drawing.Infrastructure.Identity;
using Drawing.Infrastructure.Persistence;
using Drawing.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Drawing.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DrawingDb");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DrawingDb connection string is missing.");

        services.AddDbContext<DrawingDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DrawingDbContext>());
        services.AddScoped<IArtworkRepository, ArtworkRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.SignIn.RequireConfirmedEmail = true;
        })

        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<DrawingDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IEmailDelivery, EmailDelivery>();


        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey)
            || Encoding.UTF8.GetByteCount(jwtKey) < 32
            || string.IsNullOrWhiteSpace(jwtIssuer)
            || string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("JWT configuration is missing or invalid.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,

                        ClockSkew = TimeSpan.Zero,

                        NameClaimType = JwtRegisteredClaimNames.Sub,

                        RoleClaimType = "role"
                    };


                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var tokenStamp = context.Principal?.FindFirst("stamp")?.Value;

                        if (!Guid.TryParse(userId, out var id))
                        {
                            context.Fail("Invalid user identity.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(id.ToString());

                        if (user is null || user.IsBlocked || !user.EmailConfirmed || await userManager.IsLockedOutAsync(user))
                        {
                            context.Fail("User is unavailable.");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(tokenStamp) || tokenStamp != user.SecurityStamp)
                        {
                            context.Fail("Session was revoked.");
                            return;
                        }

                        foreach (var role in context.Principal!.FindAll("role"))
                        {
                            if (!await userManager.IsInRoleAsync(user, role.Value))
                            {
                                context.Fail("Role permission was revoked.");
                                return;
                            }
                        }
                    }
                };

            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Admin));
        });

        return services;
    }
}