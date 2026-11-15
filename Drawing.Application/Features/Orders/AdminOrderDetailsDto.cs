namespace Drawing.Application.Features.Orders;

public sealed record AdminOrderDetailsDto(
    Guid Id,
    Guid BuyerUserId,
    string BuyerFullName,
    string BuyerEmail,
    string BuyerPhone,
    string DeliveryAddress,
    string DeliveryCity,
    decimal TotalAmount,
    decimal DeliveryFee,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    DeliveryDto? Delivery);
