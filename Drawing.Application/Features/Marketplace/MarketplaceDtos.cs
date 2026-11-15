using System.ComponentModel.DataAnnotations;

namespace Drawing.Application.Features.Marketplace;
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed class ArtworkSearchRequest
{
    public string? Q { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TechniqueId { get; set; }
    public Guid? SubjectId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinWidthCm { get; set; }
    public decimal? MaxWidthCm { get; set; }
    public bool AvailableOnly { get; set; } = true;
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public sealed record ArtworkCardDto(
    Guid Id,
    string Title,
    decimal Price,
    string City,
    string ArtistName,
    string SaleStatus,
    string? MainImageUrl,
    DateTime? PublishedAt);

public sealed record ArtworkDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    decimal Price,
    decimal WidthCm,
    decimal HeightCm,
    string City,
    string ArtistName,
    Guid ArtistId,
    string SaleStatus,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<string> Categories);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder);

public sealed record HomeSlideDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string ImageUrl,
    string? ButtonText,
    string? LinkUrl);

public sealed record HomeCategoryDto(
    CategoryDto Category,
    IReadOnlyList<ArtworkCardDto> Artworks);

public sealed record HomeDto(
    IReadOnlyList<HomeSlideDto> Slides,
    IReadOnlyList<HomeCategoryDto> Categories,
    IReadOnlyList<ArtworkCardDto> FeaturedArtworks);

public sealed record NavbarDto(
    IReadOnlyList<CategoryDto> Categories);

public sealed record SitePageDto(
    string Slug,
    string Title,
    string Content);

public sealed record FooterDto(
    string? Email,
    string? Phone,
    string? Instagram,
    string? Facebook,
    IReadOnlyList<SitePageDto> Links);

public sealed class ContactRequest
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Message { get; set; } = "";
}

public sealed class CategoryUpsertRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Required]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowOnHome { get; set; } = true;
}

public sealed class SlideUpsertRequest
{
    [Required, StringLength(180)]
    public string Title { get; set; } = "";

    [StringLength(400)]
    public string? Subtitle { get; set; }

    [Required]
    public string ImageKey { get; set; } = "";

    [StringLength(80)]
    public string? ButtonText { get; set; }

    [StringLength(500)]
    public string? LinkUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class SitePageUpsertRequest
{
    [Required, StringLength(120)]
    public string Slug { get; set; } = "";

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public string Content { get; set; } = "";

    public bool Published { get; set; } = true;
}