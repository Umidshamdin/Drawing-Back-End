using Drawing.Application.Abstractions.Identity;
using Drawing.Application.Abstractions.Persistence;
using Drawing.Infrastructure.Identity;
using Drawing.Infrastructure.Persistence;
using Drawing.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DrawingDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DrawingDb connection string is missing.");
        }

        services.AddDbContext<DrawingDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DrawingDbContext>());

        services.AddScoped<IArtworkRepository, ArtworkRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}