namespace Drawing.Application.Features.Artworks.DTOs;
public sealed class CreateArtworkRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public Guid? TechniqueId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? StyleId { get; set; }
    public int? CreationYear { get; set; }
    public bool IsFramed { get; set; }
    public string City { get; set; } = "Bakı";
    public string? LocationDetails { get; set; }
}