namespace Drawing.Application.Features.ReferenceData;

public interface IReferenceDataService
{
    Task<IReadOnlyList<ReferenceItemDto>> GetAsync(string type, CancellationToken ct);
    Task<Guid> SaveAsync(string type, Guid? id, ReferenceUpsertRequest request, CancellationToken ct);
    Task DeleteAsync(string type, Guid id, CancellationToken ct);
}
