using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class HomeSlide : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageKey { get; set; } = string.Empty;
    public string? ButtonText { get; set; }
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}