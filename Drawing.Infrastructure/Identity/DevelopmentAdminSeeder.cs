using Drawing.Application.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Infrastructure.Identity;
public static class DevelopmentAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var email = configuration["AdminBootstrap:Email"];
        var password = configuration["AdminBootstrap:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            FullName = "Development Administrator",
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Admin);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            throw new InvalidOperationException("Development administrator role assignment failed.");
        }
    }
}