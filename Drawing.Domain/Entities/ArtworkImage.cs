using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class ArtworkImage : BaseEntity
{
    public Guid ArtworkId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public Artwork Artwork { get; set; } = null!;
}