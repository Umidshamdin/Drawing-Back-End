using Drawing.API.Infrastructure;
using Drawing.Application.Features.Artworks.DTOs;
using Drawing.Application.Features.Artworks.Interfaces;
using Drawing.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/artist/artworks")]
public sealed class ArtistArtworkController : ControllerBase
{
    private readonly IArtworkService _artworkService;
    private readonly IMarketplaceService _marketplace;

    public ArtistArtworkController(IArtworkService artworkService, IMarketplaceService marketplace)
    {
        _artworkService = artworkService;
        _marketplace = marketplace;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArtworkRequest request, CancellationToken ct)
    {
        var result = await _artworkService.CreateDraftAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var artwork = await _artworkService.GetByIdAsync(id, ct);
        return artwork is null ? NotFound() : Ok(artwork);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateArtworkRequest request, CancellationToken ct)
    {
        await _marketplace.UpdateArtworkAsync(this.UserId(), id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _marketplace.DeleteDraftArtworkAsync(this.UserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _marketplace.SubmitArtworkAsync(this.UserId(), id, ct);
        return NoContent();
    }
}
