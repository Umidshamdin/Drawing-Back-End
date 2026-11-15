using Drawing.Application.Features.Marketplace;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;

public sealed class ContentAdminService : IContentAdminService
{
    private readonly DrawingDbContext _db;

    public ContentAdminService(DrawingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryUpsertView>> GetCategoriesAsync(CancellationToken ct)
    {
        return await _db.Categories.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CategoryUpsertView(x.Id, x.Name, x.Slug, x.Description, x.DisplayOrder, x.IsActive, x.ShowOnHome))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SlideUpsertView>> GetSlidesAsync(CancellationToken ct)
    {
        return await _db.HomeSlides.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new SlideUpsertView(x.Id, x.Title, x.Subtitle, x.ImageKey, x.ButtonText, x.LinkUrl, x.SortOrder, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PageUpsertView>> GetPagesAsync(CancellationToken ct)
    {
        return await _db.SitePages.AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new PageUpsertView(x.Id, x.Slug, x.Title, x.Content, x.Published))
            .ToListAsync(ct);
    }

    public async Task DisableCategoryAsync(Guid id, CancellationToken ct)
    {
        var category = await _db.Categories.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Kateqoriya tapılmadı.");

        category.IsActive = false;
        category.ShowOnHome = false;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DisableSlideAsync(Guid id, CancellationToken ct)
    {
        var slide = await _db.HomeSlides.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Slider tapılmadı.");

        slide.IsActive = false;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UnpublishPageAsync(Guid id, CancellationToken ct)
    {
        var page = await _db.SitePages.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException("Səhifə tapılmadı.");

        page.Published = false;

        await _db.SaveChangesAsync(ct);
    }
}
