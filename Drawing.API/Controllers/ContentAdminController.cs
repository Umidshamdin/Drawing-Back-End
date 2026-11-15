using Drawing.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public sealed class ContentAdminController : ControllerBase
{
    private readonly IContentAdminService _service;

    public ContentAdminController(IContentAdminService service)
    {
        _service = service;
    }

    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryUpsertView>> Categories(CancellationToken ct) => _service.GetCategoriesAsync(ct);

    [HttpGet("slides")]
    public Task<IReadOnlyList<SlideUpsertView>> Slides(CancellationToken ct) => _service.GetSlidesAsync(ct);

    [HttpGet("pages")]
    public Task<IReadOnlyList<PageUpsertView>> Pages(CancellationToken ct) => _service.GetPagesAsync(ct);

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DisableCategory(Guid id, CancellationToken ct)
    {
        await _service.DisableCategoryAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("slides/{id:guid}")]
    public async Task<IActionResult> DisableSlide(Guid id, CancellationToken ct)
    {
        await _service.DisableSlideAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("pages/{id:guid}")]
    public async Task<IActionResult> UnpublishPage(Guid id, CancellationToken ct)
    {
        await _service.UnpublishPageAsync(id, ct);
        return NoContent();
    }
}
