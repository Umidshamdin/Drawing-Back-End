using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class Favorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ArtworkId { get; set; }
    public Artwork Artwork { get; set; } = null!;
}