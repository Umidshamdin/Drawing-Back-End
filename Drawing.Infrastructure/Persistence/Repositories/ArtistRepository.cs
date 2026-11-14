using Drawing.Application.Abstractions.Persistence;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Persistence.Repositories;
public sealed class ArtistRepository : IArtistRepository
{
    private readonly DrawingDbContext _context;
    public ArtistRepository(DrawingDbContext context)
    {
        _context = context;
    }
    public async Task<ArtistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ArtistProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public async Task<ArtistProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ArtistProfiles.FirstOrDefaultAsync( x => x.UserId == userId, cancellationToken);
    }
    public async Task<IReadOnlyList<ArtistProfile>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ArtistProfiles
            .AsNoTracking()
            .Where(x => x.Status == ArtistStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
    public async Task AddAsync(ArtistProfile artist, CancellationToken cancellationToken = default)
    {
        await _context.ArtistProfiles.AddAsync(artist, cancellationToken);
    }
}