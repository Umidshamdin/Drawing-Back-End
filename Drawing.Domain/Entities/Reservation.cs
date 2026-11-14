using Drawing.Domain.Common;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

namespace Drawing.Domain.Entities;
public class Reservation : BaseEntity
{
    public Guid ArtworkId { get; set; }
    public Guid OrderId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ReservationStatus Status { get; private set; }= ReservationStatus.Active;
    public Artwork Artwork { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public void Expire()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("Reservation is not active.");

        if (DateTime.UtcNow < ExpiresAt)
            throw new DomainException("Reservation has not expired yet.");

        Status = ReservationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Cancel()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("Reservation is not active.");

        Status = ReservationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    public void ConvertToSale()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("Reservation is not active.");

        if (DateTime.UtcNow >= ExpiresAt)
            throw new DomainException("Reservation has expired.");

        Status = ReservationStatus.ConvertedToSale;
        UpdatedAt = DateTime.UtcNow;
    }
}