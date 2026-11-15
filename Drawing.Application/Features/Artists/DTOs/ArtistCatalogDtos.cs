using System.ComponentModel.DataAnnotations;
using Drawing.Application.Features.Marketplace;

namespace Drawing.Application.Features.Artists.DTOs;

public sealed class UpdateArtistProfileRequest
{
    [Required, StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? Biography { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;
}

public sealed record ArtistCardDto(Guid Id, string DisplayName, string City, string? ProfileImageUrl);

public sealed record ArtistPublicDto(Guid Id, string DisplayName, string? Biography, string City, string? ProfileImageUrl, IReadOnlyList<ArtworkCardDto> Artworks);

public sealed record FeaturedUpdateRequest(bool Featured);
