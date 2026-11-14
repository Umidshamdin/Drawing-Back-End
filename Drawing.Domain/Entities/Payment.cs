using Drawing.Domain.Common;
using Drawing.Domain.Enums;

namespace Drawing.Domain.Entities;
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; }= PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public Order Order { get; set; } = null!;
}