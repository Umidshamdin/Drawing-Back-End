using Drawing.Domain.Common;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

namespace Drawing.Domain.Entities;
public class Artwork : BaseEntity
{
    public Guid ArtistProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public Guid? TechniqueId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? StyleId { get; set; }
    public int? CreationYear { get; set; }
    public bool IsFramed { get; set; }
    public string City { get; set; } = "Bakı";
    public string? LocationDetails { get; set; }
    public bool IsFeatured { get; set; }
    public ArtworkModerationStatus ModerationStatus
    {
        get;
        private set;
    } = ArtworkModerationStatus.Draft;

    public ArtworkSaleStatus SaleStatus
    {
        get;
        private set;
    } = ArtworkSaleStatus.Available;
    public string? ModerationReason { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public ArtistProfile ArtistProfile { get; set; } = null!;
    public ICollection<ArtworkImage> Images { get; set; } = new List<ArtworkImage>();
    public void SubmitForReview()
    {
        if (ModerationStatus != ArtworkModerationStatus.Draft && ModerationStatus != ArtworkModerationStatus.Rejected
             && ModerationStatus != ArtworkModerationStatus.ChangesRequested)
        {
            throw new DomainException("Artwork cannot be submitted in its current state.");
        }

        if (string.IsNullOrWhiteSpace(Title) || Price <= 0 || WidthCm <= 0 || HeightCm <= 0 || Images.Count == 0)
        {
            throw new DomainException("Artwork has missing or invalid required information.");
        }

        ModerationStatus = ArtworkModerationStatus.PendingReview;
        ModerationReason = null;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Approve()
    {
        if (ModerationStatus != ArtworkModerationStatus.PendingReview)
            throw new DomainException("Only pending artworks can be approved.");

        ModerationStatus = ArtworkModerationStatus.Approved;
        ModerationReason = null;
        PublishedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    public void RequestChanges(string reason)
    {
        if (ModerationStatus != ArtworkModerationStatus.PendingReview)
            throw new DomainException("Only pending artworks can be returned for changes.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required.");

        ModerationStatus = ArtworkModerationStatus.ChangesRequested;
        ModerationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    public void Reject(string reason)
    {
        if (ModerationStatus != ArtworkModerationStatus.PendingReview)
            throw new DomainException("Only pending artworks can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        ModerationStatus = ArtworkModerationStatus.Rejected;
        ModerationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    public void Hide()
    {
        if (ModerationStatus != ArtworkModerationStatus.Approved)
            throw new DomainException("Only approved artworks can be hidden.");

        ModerationStatus = ArtworkModerationStatus.Hidden;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Reserve()
    {
        if (ModerationStatus != ArtworkModerationStatus.Approved
            || SaleStatus != ArtworkSaleStatus.Available)
        {
            throw new DomainException("Artwork is not available for reservation.");
        }

        SaleStatus = ArtworkSaleStatus.Reserved;
        UpdatedAt = DateTime.UtcNow;
    }
    public void ReleaseReservation()
    {
        if (SaleStatus != ArtworkSaleStatus.Reserved)
            throw new DomainException("Artwork is not reserved.");

        SaleStatus = ArtworkSaleStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkAsSold()
    {
        if (SaleStatus != ArtworkSaleStatus.Reserved)
            throw new DomainException("Artwork must be reserved before sale.");

        SaleStatus = ArtworkSaleStatus.Sold;
        UpdatedAt = DateTime.UtcNow;
    }
}