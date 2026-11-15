using Drawing.Application.Features.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _service;

    public ReferenceDataController(IReferenceDataService service)
    {
        _service = service;
    }

    [HttpGet("lookups/{type}")]
    public Task<IReadOnlyList<ReferenceItemDto>> Get(string type, CancellationToken ct)
    {
        return _service.GetAsync(type, ct);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/lookups/{type}")]
    public async Task<IActionResult> Create(string type, [FromBody] ReferenceUpsertRequest request, CancellationToken ct)
    {
        var id = await _service.SaveAsync(type, null, request, ct);
        return Ok(new { Id = id });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("admin/lookups/{type}/{id:guid}")]
    public async Task<IActionResult> Update(string type, Guid id, [FromBody] ReferenceUpsertRequest request, CancellationToken ct)
    {
        await _service.SaveAsync(type, id, request, ct);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("admin/lookups/{type}/{id:guid}")]
    public async Task<IActionResult> Delete(string type, Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(type, id, ct);
        return NoContent();
    }
}
