using Drawing.Application.Features.Artists.DTOs;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Application.Features.Marketplace;
using Drawing.Domain.Enums;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;

public sealed class ArtistCatalogService : IArtistCatalogService
{
    private readonly DrawingDbContext _db;

    public ArtistCatalogService(DrawingDbContext db)
    {
        _db = db;
    }

    private static ArtworkCardDto MapArtwork(Drawing.Domain.Entities.Artwork artwork)
    {
        var image = artwork.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).FirstOrDefault();
        var imageUrl = image is null ? null : "/api/v1/media/" + image.StorageKey;

        return new ArtworkCardDto(artwork.Id, artwork.Title, artwork.Price, artwork.City, artwork.ArtistProfile.DisplayName, artwork.SaleStatus.ToString(), imageUrl, artwork.PublishedAt);
    }

    public async Task<ArtistPublicDto?> GetPublicAsync(Guid artistId, CancellationToken ct)
    {
        var artist = await _db.ArtistProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == artistId && x.Status == ArtistStatus.Approved, ct);

        if (artist is null) return null;

        var artworks = await _db.Artworks.AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.ArtistProfile)
            .Where(x => x.ArtistProfileId == artistId && x.ModerationStatus == ArtworkModerationStatus.Approved)
            .OrderByDescending(x => x.PublishedAt)
            .Take(100)
            .ToListAsync(ct);

        return new ArtistPublicDto(artist.Id, artist.DisplayName, artist.Biography, artist.City, artist.ProfileImageUrl, artworks.Select(MapArtwork).ToList());
    }

    public async Task<IReadOnlyList<ArtistCardDto>> GetFeaturedAsync(CancellationToken ct)
    {
        return await _db.ArtistProfiles.AsNoTracking()
            .Where(x => x.Status == ArtistStatus.Approved && x.IsFeatured)
            .OrderByDescending(x => x.CreatedAt)
            .Take(12)
            .Select(x => new ArtistCardDto(x.Id, x.DisplayName, x.City, x.ProfileImageUrl))
            .ToListAsync(ct);
    }

    public async Task UpdateMyProfileAsync(Guid userId, UpdateArtistProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.City))
            throw new ArgumentException("Rəssam adı və şəhər tələb olunur.");

        var artist = await _db.ArtistProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Rəssam profili tapılmadı.");

        if (artist.Status == ArtistStatus.Suspended)
            throw new InvalidOperationException("Dayandırılmış rəssam profili redaktə edilə bilməz.");

        artist.DisplayName = request.DisplayName.Trim();
        artist.Biography = request.Biography?.Trim();
        artist.City = request.City.Trim();

        await _db.SaveChangesAsync(ct);
    }

    public async Task ResubmitAsync(Guid userId, CancellationToken ct)
    {
        var artist = await _db.ArtistProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Rəssam profili tapılmadı.");

        artist.Resubmit();

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetFeaturedArtistAsync(Guid artistId, bool featured, CancellationToken ct)
    {
        var artist = await _db.ArtistProfiles.FirstOrDefaultAsync(x => x.Id == artistId, ct)
            ?? throw new KeyNotFoundException("Rəssam tapılmadı.");

        if (featured && artist.Status != ArtistStatus.Approved)
            throw new InvalidOperationException("Təsdiqlənməmiş rəssam seçilmiş siyahıya əlavə edilə bilməz.");

        artist.IsFeatured = featured;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetFeaturedArtworkAsync(Guid artworkId, bool featured, CancellationToken ct)
    {
        var artwork = await _db.Artworks.Include(x => x.ArtistProfile).FirstOrDefaultAsync(x => x.Id == artworkId, ct)
            ?? throw new KeyNotFoundException("Əsər tapılmadı.");

        if (featured && (artwork.ModerationStatus != ArtworkModerationStatus.Approved || artwork.ArtistProfile.Status != ArtistStatus.Approved))
            throw new InvalidOperationException("Yalnız təsdiqlənmiş əsər seçilə bilər.");

        artwork.IsFeatured = featured;

        await _db.SaveChangesAsync(ct);
    }
}
