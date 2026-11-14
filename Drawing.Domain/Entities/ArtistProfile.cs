using Drawing.Domain.Common;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

namespace Drawing.Domain.Entities;
public class ArtistProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string City { get; set; } = "Bakı";
    public ArtistStatus Status { get; private set; } = ArtistStatus.Pending;
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public ICollection<Artwork> Artworks { get; set; }  = new List<Artwork>();
    public void Approve(Guid adminUserId)
    {
        if (Status != ArtistStatus.Pending) throw new DomainException("Only pending artist profiles can be approved.");

        Status = ArtistStatus.Approved;
        RejectionReason = null;
        ReviewedByUserId = adminUserId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Reject(Guid adminUserId, string reason)
    {
        if (Status != ArtistStatus.Pending) throw new DomainException("Only pending artist profiles can be rejected.");

        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Rejection reason is required.");

        Status = ArtistStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedByUserId = adminUserId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
