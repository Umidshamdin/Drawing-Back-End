using Drawing.Application.Features.Artworks.DTOs;

namespace Drawing.Application.Features.Artworks.Interfaces;
public interface IArtworkService
{
    Task<ArtworkDto> CreateDraftAsync(CreateArtworkRequest request, CancellationToken cancellationToken = default);
    Task<ArtworkDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtworkDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SubmitForReviewAsync(Guid id, CancellationToken cancellationToken = default);
}