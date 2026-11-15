using Drawing.Domain.Common;
using Drawing.Domain.Enums;
using Drawing.Domain.Exceptions;

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

    public void Confirm(string paymentMethod, string? transactionReference = null)
    {
        if (Status != PaymentStatus.Pending) throw new DomainException("Yalnız gözləyən ödəniş təsdiqlənə bilər.");
        if (string.IsNullOrWhiteSpace(paymentMethod)) throw new DomainException("Ödəniş üsulu tələb olunur.");

        PaymentMethod = paymentMethod.Trim();
        TransactionReference = transactionReference?.Trim();
        Status = PaymentStatus.Confirmed;
        PaidAt = DateTime.UtcNow;
    }
}