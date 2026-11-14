using Drawing.Application.Abstractions.Persistence;
using Drawing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Persistence.Repositories;
public sealed class ArtworkRepository : IArtworkRepository
{
    private readonly DrawingDbContext _context;
    public ArtworkRepository(DrawingDbContext context)
    {
        _context = context;
    }
    public async Task<Artwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Artworks.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public async Task<IReadOnlyList<Artwork>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Artworks.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
    public async Task AddAsync(Artwork artwork, CancellationToken cancellationToken = default)
    {
        await _context.Artworks.AddAsync(artwork, cancellationToken);
    }
}