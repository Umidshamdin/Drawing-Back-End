using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnHome { get; set; } = true;
    public ICollection<ArtworkCategory> ArtworkCategories { get; set; } = new List<ArtworkCategory>();
}