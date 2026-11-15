using Drawing.API.Infrastructure;
using Drawing.Application.Features.Marketplace;
using Drawing.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrderController : ControllerBase
{
    private readonly IMarketplaceService _service;

    public OrderController(IMarketplaceService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _service.CreateOrderAsync(this.UserId(), request, ct);
        return Created($"/api/v1/orders/{order.Id}", order);
    }

    [HttpGet("my")]
    public Task<PagedResult<OrderDto>> MyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return _service.MyOrdersAsync(this.UserId(), page, pageSize, ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var order = await _service.OrderAsync(this.UserId(), id, false, ct);
        return order is null ? NotFound() : Ok(order);
    }
}
