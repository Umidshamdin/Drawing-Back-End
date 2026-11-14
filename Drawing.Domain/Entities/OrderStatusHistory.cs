using Drawing.Domain.Common;
using Drawing.Domain.Enums;

namespace Drawing.Domain.Entities;
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Note { get; set; }
    public Order Order { get; set; } = null!;
}