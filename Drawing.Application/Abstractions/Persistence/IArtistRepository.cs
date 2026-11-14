using Drawing.Domain.Entities;

namespace Drawing.Application.Abstractions.Persistence;
public interface IArtistRepository
{
    Task<ArtistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArtistProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
