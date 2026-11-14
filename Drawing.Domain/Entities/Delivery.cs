using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class Delivery : BaseEntity
{
    public Guid OrderId { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DeliveryNotes { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Order Order { get; set; } = null!;
}