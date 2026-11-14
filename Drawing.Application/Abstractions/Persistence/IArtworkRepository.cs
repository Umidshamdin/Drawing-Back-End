using Drawing.Domain.Entities;

namespace Drawing.Application.Abstractions.Persistence;
public interface IArtworkRepository
{
    Task<Artwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Artwork>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Artwork artwork, CancellationToken cancellationToken = default);
}
