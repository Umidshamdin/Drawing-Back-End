using System.ComponentModel.DataAnnotations;

namespace Drawing.Application.Features.Orders;
public sealed class CreateOrderRequest
{
    [Required]
    public Guid ArtworkId { get; set; }

    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Address { get; set; } = string.Empty;

    public Guid RequestId { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = "Bakı";
}

public sealed record OrderDto(Guid Id, Guid BuyerUserId, string BuyerFullName, decimal TotalAmount, string Status, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(Guid ArtworkId, string Title, decimal UnitPrice);

public sealed class AdminOrderTransitionRequest
{
    [Required]
    public string Action { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Reason { get; set; }
    public decimal? DeliveryFee { get; set; }
    public int ReservationHours { get; set; } = 24;

    [StringLength(1000)]
    public string? PaymentInstructions { get; set; }
}
