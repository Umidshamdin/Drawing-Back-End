using Drawing.Domain.Enums;

namespace Drawing.Application.Features.Artworks.DTOs;
public sealed class ArtworkDto
{
    public Guid Id { get; set; }
    public Guid ArtistProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public string City { get; set; } = string.Empty;
    public bool IsFramed { get; set; }
    public ArtworkModerationStatus ModerationStatus { get; set; }
    public ArtworkSaleStatus SaleStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}