using Drawing.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/email-queue")]
public sealed class EmailQueueAdminController : ControllerBase
{
    private readonly IEmailQueueAdminService _service;

    public EmailQueueAdminController(IEmailQueueAdminService service)
    {
        _service = service;
    }

    [HttpGet("failed")]
    public Task<IReadOnlyList<FailedEmailDto>> Failed(CancellationToken ct) => _service.GetFailedAsync(ct);

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        await _service.RetryAsync(id, ct);
        return NoContent();
    }
}
