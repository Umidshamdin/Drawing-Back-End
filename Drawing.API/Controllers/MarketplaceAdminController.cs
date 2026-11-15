using Drawing.API.Infrastructure;
using Drawing.Application.Features.Marketplace;
using Drawing.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public sealed class MarketplaceAdminController : ControllerBase
{
    private readonly IMarketplaceService _service;

    public MarketplaceAdminController(IMarketplaceService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public Task<object> Dashboard(CancellationToken ct) => _service.DashboardAsync(ct);

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryUpsertRequest request, CancellationToken ct)
    {
        var id = await _service.SaveCategoryAsync(null, request, ct);
        return Ok(new { Id = id });
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryUpsertRequest request, CancellationToken ct)
    {
        var result = await _service.SaveCategoryAsync(id, request, ct);
        return Ok(new { Id = result });
    }

    [HttpPost("slides")]
    public async Task<IActionResult> CreateSlide([FromBody] SlideUpsertRequest request, CancellationToken ct)
    {
        var id = await _service.SaveSlideAsync(null, request, ct);
        return Ok(new { Id = id });
    }

    [HttpPut("slides/{id:guid}")]
    public async Task<IActionResult> UpdateSlide(Guid id, [FromBody] SlideUpsertRequest request, CancellationToken ct)
    {
        var result = await _service.SaveSlideAsync(id, request, ct);
        return Ok(new { Id = result });
    }

    [HttpPost("pages")]
    public async Task<IActionResult> CreatePage([FromBody] SitePageUpsertRequest request, CancellationToken ct)
    {
        var id = await _service.SavePageAsync(null, request, ct);
        return Ok(new { Id = id });
    }

    [HttpPut("pages/{id:guid}")]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] SitePageUpsertRequest request, CancellationToken ct)
    {
        var result = await _service.SavePageAsync(id, request, ct);
        return Ok(new { Id = result });
    }

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> SaveSetting(string key, [FromBody] SettingValue request, CancellationToken ct)
    {
        await _service.SetSettingAsync(key, request.Value, ct);
        return NoContent();
    }

    [HttpGet("artworks/pending")]
    public Task<IReadOnlyList<ArtworkCardDto>> PendingArtworks(CancellationToken ct)
        => _service.PendingArtworksAsync(ct);

    [HttpPost("artworks/{id:guid}/moderate")]
    public async Task<IActionResult> ModerateArtwork(Guid id, [FromBody] ArtworkDecision request, CancellationToken ct)
    {
        await _service.ReviewArtworkAsync(this.UserId(), id, request.Action, request.Reason, ct);
        return NoContent();
    }

    [HttpGet("orders")]
    public Task<IReadOnlyList<OrderDto>> Orders(CancellationToken ct) => _service.AdminOrdersAsync(ct);

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Order(Guid id, CancellationToken ct)
    {
        var order = await _service.OrderAsync(this.UserId(), id, true, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("orders/{id:guid}/transition")]
    public async Task<IActionResult> TransitionOrder(Guid id, [FromBody] AdminOrderTransitionRequest request, CancellationToken ct)
    {
        await _service.TransitionOrderAsync(this.UserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("users")]
    public Task<IReadOnlyList<object>> Users(CancellationToken ct) => _service.AdminUsersAsync(ct);

    [HttpPatch("users/{id:guid}/blocked")]
    public async Task<IActionResult> BlockUser(Guid id, [FromBody] BlockRequest request, CancellationToken ct)
    {
        await _service.SetUserBlockedAsync(this.UserId(), id, request.Blocked, ct);
        return NoContent();
    }

    [HttpGet("contact-messages")]
    public Task<IReadOnlyList<object>> ContactMessages(CancellationToken ct) => _service.AdminContactsAsync(ct);

    [HttpPatch("contact-messages/{id:guid}/read")]
    public async Task<IActionResult> MarkContactRead(Guid id, CancellationToken ct)
    {
        await _service.SetContactReadAsync(id, ct);
        return NoContent();
    }

    public sealed record SettingValue(string Value);
    public sealed record ArtworkDecision(string Action, string? Reason);
    public sealed record BlockRequest(bool Blocked);
}
