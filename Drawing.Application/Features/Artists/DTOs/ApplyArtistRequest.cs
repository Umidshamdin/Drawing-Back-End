namespace Drawing.Application.Features.Artists.DTOs;
public sealed class ApplyArtistRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string City { get; set; } = "Bakı";
    public string? ProfileImageUrl { get; set; }
}