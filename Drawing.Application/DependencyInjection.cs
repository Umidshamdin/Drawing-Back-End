using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Application.Features.Artists.Services;
using Drawing.Application.Features.Artworks.Interfaces;
using Drawing.Application.Features.Artworks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IArtworkService, ArtworkService>();

        services.AddScoped<IArtistService, ArtistService>();

        return services;
    }
}