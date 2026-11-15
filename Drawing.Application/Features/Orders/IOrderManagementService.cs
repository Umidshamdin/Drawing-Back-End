namespace Drawing.Application.Features.Orders;

public interface IOrderManagementService
{
    Task<OrderTrackingDto?> GetTrackingAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<IReadOnlyList<ArtistSaleDto>> GetArtistSalesAsync(Guid userId, CancellationToken ct);
    Task UpdateDeliveryAsync(Guid adminId, Guid orderId, DeliveryUpsertRequest request, CancellationToken ct);
    Task<Guid> AddNoteAsync(Guid adminId, Guid orderId, AddOrderNoteRequest request, CancellationToken ct);
    Task<IReadOnlyList<AdminOrderNoteDto>> GetNotesAsync(Guid orderId, CancellationToken ct);
    Task<AdminOrderDetailsDto?> GetAdminDetailsAsync(Guid orderId, CancellationToken ct);
}
