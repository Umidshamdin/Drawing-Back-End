using Drawing.Application.Features.Artists.DTOs;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/artists")]
public sealed class AdminArtistsController : ControllerBase
{
    private readonly IArtistService _artistService;
    public AdminArtistsController(IArtistService artistService)
    {
        _artistService = artistService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await _artistService.GetPendingAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _artistService.ApproveAsync(id, cancellationToken);

            return Ok(new
            {
                Message = "Rəssam müraciəti təsdiqləndi."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectArtistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _artistService.RejectAsync(id, request.Reason, cancellationToken);

            return Ok(new
            {
                Message = "Rəssam müraciəti rədd edildi."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }
}