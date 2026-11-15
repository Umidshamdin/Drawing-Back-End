namespace Drawing.Application.Features.Marketplace;

public interface IContentAdminService
{
    Task<IReadOnlyList<CategoryUpsertView>> GetCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<SlideUpsertView>> GetSlidesAsync(CancellationToken ct);
    Task<IReadOnlyList<PageUpsertView>> GetPagesAsync(CancellationToken ct);
    Task DisableCategoryAsync(Guid id, CancellationToken ct);
    Task DisableSlideAsync(Guid id, CancellationToken ct);
    Task UnpublishPageAsync(Guid id, CancellationToken ct);
}

public sealed record CategoryUpsertView(Guid Id, string Name, string Slug, string? Description, int DisplayOrder, bool IsActive, bool ShowOnHome);

public sealed record SlideUpsertView(Guid Id, string Title, string? Subtitle, string ImageKey, string? ButtonText, string? LinkUrl, int SortOrder, bool IsActive);

public sealed record PageUpsertView(Guid Id, string Slug, string Title, string Content, bool Published);
