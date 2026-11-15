using Drawing.API.Infrastructure;
using Drawing.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class OrderManagementController : ControllerBase
{
    private readonly IOrderManagementService _service;

    public OrderManagementController(IOrderManagementService service)
    {
        _service = service;
    }

    [HttpGet("orders/{id:guid}/tracking")]
    public async Task<IActionResult> Tracking(Guid id, CancellationToken ct)
    {
        var result = await _service.GetTrackingAsync(id, this.UserId(), User.IsInRole("Admin"), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("artist/sales")]
    public Task<IReadOnlyList<ArtistSaleDto>> ArtistSales(CancellationToken ct)
    {
        return _service.GetArtistSalesAsync(this.UserId(), ct);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("admin/orders/{id:guid}/delivery")]
    public async Task<IActionResult> UpdateDelivery(Guid id, [FromBody] DeliveryUpsertRequest request, CancellationToken ct)
    {
        await _service.UpdateDeliveryAsync(this.UserId(), id, request, ct);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/orders/{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddOrderNoteRequest request, CancellationToken ct)
    {
        var noteId = await _service.AddNoteAsync(this.UserId(), id, request, ct);
        return Ok(new { Id = noteId });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/orders/{id:guid}/notes")]
    public Task<IReadOnlyList<AdminOrderNoteDto>> GetNotes(Guid id, CancellationToken ct)
    {
        return _service.GetNotesAsync(id, ct);
    }
}
