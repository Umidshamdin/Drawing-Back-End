using Drawing.Application.Features.Artists.DTOs;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/artists")]
public sealed class ArtistsController : ControllerBase
{
    private readonly IArtistService _artistService;
    public ArtistsController(IArtistService artistService)
    {
        _artistService = artistService;
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyArtistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _artistService.ApplyAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _artistService.GetMyProfileAsync(cancellationToken);

        if (result is null)
            return NotFound(new
            {
                Message = "Rəssam profili tapılmadı."
            });

        return Ok(result);
    }
}