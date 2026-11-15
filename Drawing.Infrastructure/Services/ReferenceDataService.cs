using Drawing.Application.Features.ReferenceData;
using Drawing.Domain.Common;
using Drawing.Domain.Entities;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;

public sealed class ReferenceDataService : IReferenceDataService
{
    private readonly DrawingDbContext _db;

    public ReferenceDataService(DrawingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReferenceItemDto>> GetAsync(string type, CancellationToken ct)
    {
        return type.ToLowerInvariant() switch
        {
            "techniques" => await _db.Techniques.AsNoTracking().OrderBy(x => x.Name).Select(x => new ReferenceItemDto(x.Id, x.Name)).ToListAsync(ct),
            "materials" => await _db.Materials.AsNoTracking().OrderBy(x => x.Name).Select(x => new ReferenceItemDto(x.Id, x.Name)).ToListAsync(ct),
            "subjects" => await _db.Subjects.AsNoTracking().OrderBy(x => x.Name).Select(x => new ReferenceItemDto(x.Id, x.Name)).ToListAsync(ct),
            "styles" => await _db.ArtworkStyles.AsNoTracking().OrderBy(x => x.Name).Select(x => new ReferenceItemDto(x.Id, x.Name)).ToListAsync(ct),
            _ => throw new ArgumentException("Yanlış məlumat tipi.")
        };
    }

    public async Task<Guid> SaveAsync(string type, Guid? id, ReferenceUpsertRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Ad tələb olunur.");

        var name = request.Name.Trim();

        return type.ToLowerInvariant() switch
        {
            "techniques" => await SaveEntityAsync(_db.Techniques, id, () => new Technique(), (x, n) => x.Name = n, name, ct),
            "materials" => await SaveEntityAsync(_db.Materials, id, () => new Material(), (x, n) => x.Name = n, name, ct),
            "subjects" => await SaveEntityAsync(_db.Subjects, id, () => new Subject(), (x, n) => x.Name = n, name, ct),
            "styles" => await SaveEntityAsync(_db.ArtworkStyles, id, () => new ArtworkStyle(), (x, n) => x.Name = n, name, ct),
            _ => throw new ArgumentException("Yanlış məlumat tipi.")
        };
    }

    private async Task<Guid> SaveEntityAsync<T>(DbSet<T> set, Guid? id, Func<T> factory, Action<T, string> setName, string name, CancellationToken ct) where T : BaseEntity
    {
        T entity;

        if (id.HasValue)
        {
            entity = await set.FindAsync(new object[] { id.Value }, ct)
                ?? throw new KeyNotFoundException("Məlumat tapılmadı.");
        }
        else
        {
            entity = factory();
            set.Add(entity);
        }

        setName(entity, name);

        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task DeleteAsync(string type, Guid id, CancellationToken ct)
    {
        switch (type.ToLowerInvariant())
        {
            case "techniques":
                await DeleteEntityAsync(_db.Techniques, id, ct);
                break;

            case "materials":
                await DeleteEntityAsync(_db.Materials, id, ct);
                break;

            case "subjects":
                await DeleteEntityAsync(_db.Subjects, id, ct);
                break;

            case "styles":
                await DeleteEntityAsync(_db.ArtworkStyles, id, ct);
                break;

            default:
                throw new ArgumentException("Yanlış məlumat tipi.");
        }
    }

    private async Task DeleteEntityAsync<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : BaseEntity
    {
        var entity = await set.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Məlumat tapılmadı.");

        set.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}
