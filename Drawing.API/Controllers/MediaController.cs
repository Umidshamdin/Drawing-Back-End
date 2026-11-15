using Drawing.API.Infrastructure;
using Drawing.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Controllers;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly SafeImageService _storage;
    private readonly IMarketplaceService _marketplace;

    public MediaController(SafeImageService storage, IMarketplaceService marketplace)
    {
        _storage = storage;
        _marketplace = marketplace;
    }

    [HttpGet("artworks/{filename}")]
    public IActionResult GetArtworkImage(string filename)
    {
        if (filename != Path.GetFileName(filename) || filename.Contains("..")) return BadRequest();

        var path = _storage.ResolvePath("artworks/" + filename);
        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, GetMimeType(filename), enableRangeProcessing: true);
    }

    [HttpGet("slides/{filename}")]
    public IActionResult GetSlideImage(string filename)
    {
        if (filename != Path.GetFileName(filename) || filename.Contains("..")) return BadRequest();

        var path = _storage.ResolvePath("slides/" + filename);
        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, GetMimeType(filename), enableRangeProcessing: true);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("admin/slides")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadSlide(IFormFile file, CancellationToken ct)
    {
        var key = await _storage.SaveAsync(file, ct, "slides");
        return Ok(new { ImageKey = key, Url = "/api/v1/media/" + key });
    }

    [Authorize]
    [HttpPost("artist/artworks/{artworkId:guid}")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadArtworkImage(Guid artworkId, IFormFile file, [FromQuery] bool primary, CancellationToken ct)
    {
        var key = await _storage.SaveAsync(file, ct, "artworks");

        try
        {
            var url = await _marketplace.AddArtworkImageAsync(this.UserId(), artworkId, key, primary, ct);
            return Ok(new { Url = url });
        }
        catch
        {
            System.IO.File.Delete(_storage.ResolvePath(key));
            throw;
        }
    }

    [Authorize]
    [HttpDelete("artist/artworks/{artworkId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteArtworkImage(Guid artworkId, Guid imageId, CancellationToken ct)
    {
        await _marketplace.DeleteArtworkImageAsync(this.UserId(), artworkId, imageId, ct);
        return NoContent();
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
}
