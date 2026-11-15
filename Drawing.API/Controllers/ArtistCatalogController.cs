using Drawing.API.Infrastructure;
using Drawing.Application.Features.Artists.DTOs;
using Drawing.Application.Features.Artists.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ArtistCatalogController : ControllerBase
{
    private readonly IArtistCatalogService _service;

    public ArtistCatalogController(IArtistCatalogService service)
    {
        _service = service;
    }

    [HttpGet("artists/featured")]
    public Task<IReadOnlyList<ArtistCardDto>> Featured(CancellationToken ct) => _service.GetFeaturedAsync(ct);

    [HttpGet("artists/{id:guid}")]
    public async Task<IActionResult> PublicProfile(Guid id, CancellationToken ct)
    {
        var artist = await _service.GetPublicAsync(id, ct);
        return artist is null ? NotFound() : Ok(artist);
    }

    [Authorize]
    [HttpPut("artists/me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateArtistProfileRequest request, CancellationToken ct)
    {
        await _service.UpdateMyProfileAsync(this.UserId(), request, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("artists/me/resubmit")]
    public async Task<IActionResult> Resubmit(CancellationToken ct)
    {
        await _service.ResubmitAsync(this.UserId(), ct);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("admin/home/featured-artists/{id:guid}")]
    public async Task<IActionResult> SetFeaturedArtist(Guid id, [FromBody] FeaturedUpdateRequest request, CancellationToken ct)
    {
        await _service.SetFeaturedArtistAsync(id, request.Featured, ct);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("admin/home/featured-artworks/{id:guid}")]
    public async Task<IActionResult> SetFeaturedArtwork(Guid id, [FromBody] FeaturedUpdateRequest request, CancellationToken ct)
    {
        await _service.SetFeaturedArtworkAsync(id, request.Featured, ct);
        return NoContent();
    }
}
