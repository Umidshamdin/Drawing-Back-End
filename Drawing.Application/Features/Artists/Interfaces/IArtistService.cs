using Drawing.Application.Features.Artists.DTOs;

namespace Drawing.Application.Features.Artists.Interfaces;
public interface IArtistService
{
    Task<ArtistProfileDto> ApplyAsync(ApplyArtistRequest request, CancellationToken cancellationToken = default);
    Task<ArtistProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtistProfileDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid artistId, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid artistId, string reason, CancellationToken cancellationToken = default);
}