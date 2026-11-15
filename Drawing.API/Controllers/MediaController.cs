using Drawing.API.Infrastructure;
using Drawing.Application.Features.Artists.Interfaces;
using Drawing.Application.Features.Marketplace;
using Drawing.Domain.Enums;
using Drawing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly SafeImageService _storage;
    private readonly IMarketplaceService _marketplace;
    private readonly IArtistCatalogService _artists;
    private readonly DrawingDbContext _db;

    public MediaController(SafeImageService storage, IMarketplaceService marketplace, IArtistCatalogService artists, DrawingDbContext db)
    {
        _storage = storage;
        _marketplace = marketplace;
        _artists = artists;
        _db = db;
    }

    private static string GetMimeType(string filename)
    {
        return Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private IActionResult Serve(string key)
    {
        var path = _storage.ResolvePath(key);

        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, GetMimeType(path), enableRangeProcessing: true);
    }


    [HttpGet("thumbnails/{filename}")]
    public async Task<IActionResult> GetThumbnail(string filename, CancellationToken ct)
    {
        if (filename != Path.GetFileName(filename) || Path.GetExtension(filename).ToLowerInvariant() != ".webp")
            return BadRequest();

        var name = Path.GetFileNameWithoutExtension(filename);

        if (!Guid.TryParseExact(name, "N", out _)) return BadRequest();

        var possibleKeys = new[]
        {
        $"artworks/{name}.jpg",
        $"artworks/{name}.jpeg",
        $"artworks/{name}.png",
        $"artworks/{name}.webp"
    };

        var userId = CurrentUserId();
        var admin = User.IsInRole("Admin");

        var visible = await _db.ArtworkImages.AsNoTracking()
            .AnyAsync(x => possibleKeys.Contains(x.StorageKey) &&
                (
                    (x.Artwork.ModerationStatus == ArtworkModerationStatus.Approved && x.Artwork.ArtistProfile.Status == ArtistStatus.Approved) ||
                    x.Artwork.ArtistProfile.UserId == userId ||
                    admin
                ), ct);

        return visible ? Serve("thumbnails/" + filename) : NotFound();
    }


    private Guid? CurrentUserId()
    {
        var value = User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    [HttpGet("artworks/{filename}")]
    public async Task<IActionResult> GetArtworkImage(string filename, CancellationToken ct)
    {
        if (filename != Path.GetFileName(filename)) return BadRequest();

        var key = "artworks/" + filename;
        var userId = CurrentUserId();
        var admin = User.IsInRole("Admin");

        var visible = await _db.ArtworkImages.AsNoTracking()
            .AnyAsync(x => x.StorageKey == key &&
                (
                    (x.Artwork.ModerationStatus == ArtworkModerationStatus.Approved && x.Artwork.ArtistProfile.Status == ArtistStatus.Approved) ||
                    x.Artwork.ArtistProfile.UserId == userId ||
                    admin
                ), ct);

        return visible ? Serve(key) : NotFound();
    }

    [HttpGet("slides/{filename}")]
    public async Task<IActionResult> GetSlideImage(string filename, CancellationToken ct)
    {
        if (filename != Path.GetFileName(filename)) return BadRequest();

        var key = "slides/" + filename;

        var visible = await _db.HomeSlides.AsNoTracking()
            .AnyAsync(x => x.ImageKey == key && x.IsActive, ct);

        return visible || User.IsInRole("Admin") ? Serve(key) : NotFound();
    }

    [HttpGet("profiles/{filename}")]
    public async Task<IActionResult> GetProfileImage(string filename, CancellationToken ct)
    {
        if (filename != Path.GetFileName(filename)) return BadRequest();

        var key = "profiles/" + filename;
        var url = "/api/v1/media/" + key;
        var userId = CurrentUserId();

        var visible = await _db.ArtistProfiles.AsNoTracking()
            .AnyAsync(x => x.ProfileImageUrl == url &&
                (x.Status == ArtistStatus.Approved || x.UserId == userId || User.IsInRole("Admin")), ct);

        return visible ? Serve(key) : NotFound();
    }

    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("public-write")]
    [HttpPost("admin/slides")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadSlide([FromForm] IFormFile file, CancellationToken ct)
    {
        var key = await _storage.SaveAsync(file, ct, "slides");

        return Ok(new { ImageKey = key, Url = "/api/v1/media/" + key });
    }

    [Authorize]
    [EnableRateLimiting("public-write")]
    [HttpPost("artist/artworks/{artworkId:guid}")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadArtworkImage(Guid artworkId, [FromForm] IFormFile file, [FromQuery] bool primary, CancellationToken ct)
    {
        var key = await _storage.SaveAsync(file, ct, "artworks");

        try
        {
            var url = await _marketplace.AddArtworkImageAsync(this.UserId(), artworkId, key, primary, ct);
            return Ok(new { Url = url });
        }
        catch
        {
            _storage.DeleteFiles(key);
            throw;
        }
    }

    [Authorize]
    [EnableRateLimiting("public-write")]
    [HttpPost("artist/profile-image")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file, CancellationToken ct)
    {
        var key = await _storage.SaveAsync(file, ct, "profiles");
        var url = "/api/v1/media/" + key;

        try
        {
            await _artists.SetProfileImageAsync(this.UserId(), url, ct);
            return Ok(new { Url = url });
        }
        catch
        {
            _storage.DeleteFiles(key);
            throw;
        }
    }


    [Authorize]
    [HttpDelete("artist/artworks/{artworkId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteArtworkImage(Guid artworkId, Guid imageId, CancellationToken ct)
    {
        var key = await _db.ArtworkImages.AsNoTracking()
            .Where(x => x.Id == imageId && x.ArtworkId == artworkId)
            .Select(x => x.StorageKey)
            .FirstOrDefaultAsync(ct);

        await _marketplace.DeleteArtworkImageAsync(this.UserId(), artworkId, imageId, ct);

        if (!string.IsNullOrWhiteSpace(key)) _storage.DeleteFiles(key);

        return NoContent();
    }

}
