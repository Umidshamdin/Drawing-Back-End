using Drawing.Domain.Enums;

namespace Drawing.Application.Features.Artists.DTOs;
public sealed class ArtistProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string City { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public ArtistStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}