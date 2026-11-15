using Drawing.Application.Features.Artists.DTOs;

namespace Drawing.Application.Features.Artists.Interfaces;
public interface IArtistCatalogService
{
    Task<ArtistPublicDto?> GetPublicAsync(Guid artistId, CancellationToken ct);
    Task<IReadOnlyList<ArtistCardDto>> GetFeaturedAsync(CancellationToken ct);
    Task UpdateMyProfileAsync(Guid userId, UpdateArtistProfileRequest request, CancellationToken ct);
    Task ResubmitAsync(Guid userId, CancellationToken ct);
    Task SetFeaturedArtistAsync(Guid artistId, bool featured, CancellationToken ct);
    Task SetFeaturedArtworkAsync(Guid artworkId, bool featured, CancellationToken ct);
}
