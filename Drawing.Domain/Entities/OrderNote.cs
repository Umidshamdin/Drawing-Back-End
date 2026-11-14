using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class OrderNote : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid AdminUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Order Order { get; set; } = null!;
}