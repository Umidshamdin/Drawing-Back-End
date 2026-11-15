using Drawing.Domain.Common;

namespace Drawing.Domain.Entities;
public class SitePage : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Published { get; set; }
}