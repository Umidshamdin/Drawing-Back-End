using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.API.Infrastructure;

public sealed class OrphanMediaCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrphanMediaCleanupWorker> _logger;

    public OrphanMediaCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<OrphanMediaCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
                await CleanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Media cleanup failed.");
            }
        }
    }

    private async Task CleanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DrawingDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<SafeImageService>();
        var folders = new[] { "artworks", "slides", "profiles", "thumbnails" };
        var cutoff = DateTime.UtcNow.AddDays(-2);

        foreach (var folder in folders)
        {
            var directory = Path.Combine(storage.StorageRoot, folder);

            if (!Directory.Exists(directory)) continue;

            var files = Directory.EnumerateFiles(directory)
                .Where(x => File.GetLastWriteTimeUtc(x) < cutoff)
                .Take(100)
                .ToList();

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                var filename = Path.GetFileName(file);
                var key = folder + "/" + filename;

                bool referenced;

                switch (folder)
                {
                    case "artworks":
                        referenced = await db.ArtworkImages.AnyAsync(x => x.StorageKey == key, ct);
                        break;

                    case "slides":
                        referenced = await db.HomeSlides.AnyAsync(x => x.ImageKey == key, ct);
                        break;

                    case "profiles":
                        var url = "/api/v1/media/" + key;
                        referenced = await db.ArtistProfiles.AnyAsync(x => x.ProfileImageUrl == url, ct);
                        break;

                    default:
                        var name = Path.GetFileNameWithoutExtension(filename);
                        var keys = new[]
                        {
                            $"artworks/{name}.jpg",
                            $"artworks/{name}.jpeg",
                            $"artworks/{name}.png",
                            $"artworks/{name}.webp"
                        };

                        referenced = await db.ArtworkImages.AnyAsync(x => keys.Contains(x.StorageKey), ct);
                        break;
                }

                if (referenced) continue;

                try
                {
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Unused media file could not be deleted.");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Media cleanup permission denied.");
                }
            }
        }
    }
}
