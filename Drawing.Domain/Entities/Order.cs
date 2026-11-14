using Drawing.Domain.Common;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

namespace Drawing.Domain.Entities;
public class Order : BaseEntity
{
    public Guid BuyerUserId { get; set; }
    public string BuyerFullName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = "Bakı";
    public decimal TotalAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? RejectionReason { get; private set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }
    public void RequestPayment()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException("Order must be confirmed first.");

        Status = OrderStatus.AwaitingPayment;
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.AwaitingPayment)
            throw new DomainException("Order is not awaiting payment.");

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }
    public void StartDeliveryPreparation()
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException("Order must be paid first.");

        Status = OrderStatus.PreparingDelivery;
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkAsShipped()
    {
        if (Status != OrderStatus.PreparingDelivery)
            throw new DomainException("Order is not ready for shipping.");

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Order has not been shipped.");

        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Reject(string reason)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        Status = OrderStatus.Rejected;
        RejectionReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    public void Cancel()
    {
        if (Status != OrderStatus.Pending
            && Status != OrderStatus.Confirmed
            && Status != OrderStatus.AwaitingPayment)
        {
            throw new DomainException("Order cannot be cancelled in its current state.");
        }

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}