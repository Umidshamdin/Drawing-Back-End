using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ArtworkId { get; set; }
    public string ArtworkTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public Order Order { get; set; } = null!;
    public Artwork Artwork { get; set; } = null!;
}
