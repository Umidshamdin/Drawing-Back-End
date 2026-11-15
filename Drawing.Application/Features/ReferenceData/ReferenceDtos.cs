using System.ComponentModel.DataAnnotations;

namespace Drawing.Application.Features.ReferenceData;
public sealed record ReferenceItemDto(Guid Id, string Name);
public sealed class ReferenceUpsertRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
