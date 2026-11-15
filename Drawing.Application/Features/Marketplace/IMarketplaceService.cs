using Drawing.Application.Features.Artworks.DTOs;
using Drawing.Application.Features.Orders;

namespace Drawing.Application.Features.Marketplace;
public interface IMarketplaceService
{
    Task<HomeDto> HomeAsync(CancellationToken ct);
    Task<NavbarDto> NavbarAsync(CancellationToken ct);
    Task<FooterDto> FooterAsync(CancellationToken ct);
    Task<PagedResult<ArtworkCardDto>> SearchAsync(ArtworkSearchRequest request, CancellationToken ct);
    Task<ArtworkDetailsDto?> ArtworkAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CategoryDto>> CategoriesAsync(CancellationToken ct);
    Task<SitePageDto?> PageAsync(string slug, CancellationToken ct);
    Task ContactAsync(ContactRequest request, CancellationToken ct);
    Task<Guid> SaveCategoryAsync(Guid? id, CategoryUpsertRequest request, CancellationToken ct);
    Task<Guid> SaveSlideAsync(Guid? id, SlideUpsertRequest request, CancellationToken ct);
    Task<Guid> SavePageAsync(Guid? id, SitePageUpsertRequest request, CancellationToken ct);
    Task SetSettingAsync(string key, string value, CancellationToken ct);
    Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken ct);
    Task<PagedResult<OrderDto>> MyOrdersAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<OrderDto?> OrderAsync(Guid userId, Guid id, bool admin, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> AdminOrdersAsync(CancellationToken ct);
    Task TransitionOrderAsync(Guid adminId, Guid id, AdminOrderTransitionRequest request, CancellationToken ct);
    Task<bool> ToggleFavoriteAsync(Guid userId, Guid artworkId, CancellationToken ct);
    Task<IReadOnlyList<ArtworkCardDto>> FavoritesAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ArtworkCardDto>> ArtistArtworksAsync(Guid userId, CancellationToken ct);
    Task UpdateArtworkAsync(Guid userId, Guid artworkId, CreateArtworkRequest request, CancellationToken ct);
    Task DeleteDraftArtworkAsync(Guid userId, Guid artworkId, CancellationToken ct);
    Task SubmitArtworkAsync(Guid userId, Guid artworkId, CancellationToken ct);
    Task SetArtworkCategoriesAsync(Guid userId, Guid artworkId, IReadOnlyList<Guid> categoryIds, CancellationToken ct);
    Task<string> AddArtworkImageAsync(Guid userId, Guid artworkId, string imageKey, bool primary, CancellationToken ct);
    Task DeleteArtworkImageAsync(Guid userId, Guid artworkId, Guid imageId, CancellationToken ct);
    Task ApproveArtworkAsync(Guid adminId, Guid id, CancellationToken ct);
    Task ReviewArtworkAsync(Guid adminId, Guid id, string action, string? reason, CancellationToken ct);
    Task<IReadOnlyList<ArtworkCardDto>> PendingArtworksAsync(CancellationToken ct);
    Task<object> DashboardAsync(CancellationToken ct);
    Task<IReadOnlyList<object>> AdminUsersAsync(CancellationToken ct);
    Task SetUserBlockedAsync(Guid adminId, Guid userId, bool blocked, CancellationToken ct);
    Task<IReadOnlyList<object>> AdminContactsAsync(CancellationToken ct);
    Task SetContactReadAsync(Guid id, CancellationToken ct);
}