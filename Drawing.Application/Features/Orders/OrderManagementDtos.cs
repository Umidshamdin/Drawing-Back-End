using System.ComponentModel.DataAnnotations;

namespace Drawing.Application.Features.Orders;

public sealed record OrderHistoryDto(string? PreviousStatus, string NewStatus, DateTime ChangedAt, string? Note);

public sealed record DeliveryDto(string? CourierName, string? TrackingNumber, string? DeliveryNotes, DateTime? EstimatedDeliveryAt, DateTime? DeliveredAt);

public sealed record OrderTrackingDto(OrderDto Order, DeliveryDto? Delivery, IReadOnlyList<OrderHistoryDto> History);

public sealed record ArtistSaleDto(Guid OrderId, Guid ArtworkId, string ArtworkTitle, decimal Amount, string Status, DateTime CreatedAt);

public sealed record AdminOrderNoteDto(Guid Id, Guid AdminUserId, string Content, DateTime CreatedAt);

public sealed class DeliveryUpsertRequest
{
    [StringLength(150)]
    public string? CourierName { get; set; }

    [StringLength(200)]
    public string? TrackingNumber { get; set; }

    [StringLength(2000)]
    public string? DeliveryNotes { get; set; }

    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public sealed class AddOrderNoteRequest
{
    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
