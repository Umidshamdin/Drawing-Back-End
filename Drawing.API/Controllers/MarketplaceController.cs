using Drawing.API.Infrastructure;
using Drawing.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _service;

    public MarketplaceController(IMarketplaceService service)
    {
        _service = service;
    }

    [HttpGet("home")]
    public Task<HomeDto> Home(CancellationToken ct) => _service.HomeAsync(ct);

    [HttpGet("navbar")]
    public Task<NavbarDto> Navbar(CancellationToken ct) => _service.NavbarAsync(ct);

    [HttpGet("footer")]
    public Task<FooterDto> Footer(CancellationToken ct) => _service.FooterAsync(ct);

    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) => _service.CategoriesAsync(ct);

    [HttpGet("artworks")]
    public Task<PagedResult<ArtworkCardDto>> Search([FromQuery] ArtworkSearchRequest request, CancellationToken ct)
        => _service.SearchAsync(request, ct);

    [HttpGet("artworks/{id:guid}")]
    public async Task<IActionResult> Artwork(Guid id, CancellationToken ct)
    {
        var artwork = await _service.ArtworkAsync(id, ct);
        return artwork is null ? NotFound() : Ok(artwork);
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> Page(string slug, CancellationToken ct)
    {
        var page = await _service.PageAsync(slug, ct);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost("contact")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Contact([FromBody] ContactRequest request, CancellationToken ct)
    {
        await _service.ContactAsync(request, ct);
        return Accepted(new { Message = "Müraciət qəbul edildi." });
    }

    [Authorize]
    [HttpPost("favorites/{artworkId:guid}/toggle")]
    public async Task<IActionResult> ToggleFavorite(Guid artworkId, CancellationToken ct)
    {
        var isFavorite = await _service.ToggleFavoriteAsync(this.UserId(), artworkId, ct);
        return Ok(new { IsFavorite = isFavorite });
    }

    [Authorize]
    [HttpGet("favorites")]
    public Task<IReadOnlyList<ArtworkCardDto>> Favorites(CancellationToken ct)
        => _service.FavoritesAsync(this.UserId(), ct);

    [Authorize]
    [HttpGet("artist/artworks")]
    public Task<IReadOnlyList<ArtworkCardDto>> MyArtworks(CancellationToken ct)
        => _service.ArtistArtworksAsync(this.UserId(), ct);

    [Authorize]
    [HttpPut("artist/artworks/{id:guid}/categories")]
    public async Task<IActionResult> SetCategories(Guid id, [FromBody] List<Guid> categoryIds, CancellationToken ct)
    {
        await _service.SetArtworkCategoriesAsync(this.UserId(), id, categoryIds, ct);
        return NoContent();
    }
}
