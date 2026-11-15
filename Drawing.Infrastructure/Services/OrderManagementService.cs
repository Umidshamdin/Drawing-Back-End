using Drawing.Application.Features.Orders;
using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;

public sealed class OrderManagementService : IOrderManagementService
{
    private readonly DrawingDbContext _db;

    public OrderManagementService(DrawingDbContext db)
    {
        _db = db;
    }

    public async Task<OrderTrackingDto?> GetTrackingAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().Include(x => x.Items).Where(x => x.Id == orderId);

        if (!isAdmin) query = query.Where(x => x.BuyerUserId == userId);

        var order = await query.FirstOrDefaultAsync(ct);

        if (order is null) return null;

        var orderDto = new OrderDto(order.Id, order.BuyerUserId, order.BuyerFullName, order.TotalAmount, order.Status.ToString(), order.CreatedAt,
            order.Items.Select(x => new OrderItemDto(x.ArtworkId, x.ArtworkTitle, x.UnitPrice)).ToList());

        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        var deliveryDto = delivery is null
            ? null
            : new DeliveryDto(delivery.CourierName, delivery.TrackingNumber, delivery.DeliveryNotes, delivery.EstimatedDeliveryAt, delivery.DeliveredAt);

        var history = await _db.OrderStatusHistories.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return new OrderTrackingDto(orderDto, deliveryDto,
            history.Select(x => new OrderHistoryDto(x.PreviousStatus?.ToString(), x.NewStatus.ToString(), x.CreatedAt, x.Note)).ToList());
    }

    public async Task<IReadOnlyList<ArtistSaleDto>> GetArtistSalesAsync(Guid userId, CancellationToken ct)
    {
        var items = await _db.OrderItems.AsNoTracking()
            .Include(x => x.Order)
            .Where(x => x.Artwork.ArtistProfile.UserId == userId)
            .OrderByDescending(x => x.Order.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return items.Select(x => new ArtistSaleDto(x.OrderId, x.ArtworkId, x.ArtworkTitle, x.UnitPrice, x.Order.Status.ToString(), x.Order.CreatedAt)).ToList();
    }


    public async Task<AdminOrderDetailsDto?> GetAdminDetailsAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (order is null) return null;

        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        var deliveryDto = delivery is null
            ? null
            : new DeliveryDto(delivery.CourierName, delivery.TrackingNumber, delivery.DeliveryNotes, delivery.EstimatedDeliveryAt, delivery.DeliveredAt);

        var items = order.Items.Select(x => new OrderItemDto(x.ArtworkId, x.ArtworkTitle, x.UnitPrice)).ToList();

        return new AdminOrderDetailsDto(
            order.Id,
            order.BuyerUserId,
            order.BuyerFullName,
            order.BuyerEmail,
            order.BuyerPhone,
            order.DeliveryAddress,
            order.DeliveryCity,
            order.TotalAmount,
            order.DeliveryFee,
            order.Status.ToString(),
            order.CreatedAt,
            items,
            deliveryDto);
    }

    public async Task UpdateDeliveryAsync(Guid adminId, Guid orderId, DeliveryUpsertRequest request, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Sifariş tapılmadı.");

        if (order.Status is not (OrderStatus.Paid or OrderStatus.PreparingDelivery or OrderStatus.Shipped or OrderStatus.Completed))
            throw new InvalidOperationException("Bu sifariş üçün çatdırılma məlumatı yenilənə bilməz.");

        var delivery = await _db.Deliveries.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        if (delivery is null)
        {
            delivery = new Delivery { OrderId = orderId };
            _db.Deliveries.Add(delivery);
        }

        delivery.CourierName = request.CourierName?.Trim();
        delivery.TrackingNumber = request.TrackingNumber?.Trim();
        delivery.DeliveryNotes = request.DeliveryNotes?.Trim();
        delivery.EstimatedDeliveryAt = request.EstimatedDeliveryAt;
        delivery.DeliveredAt = request.DeliveredAt;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            Action = "Delivery.Update",
            EntityName = "Order",
            EntityId = orderId.ToString()
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddNoteAsync(Guid adminId, Guid orderId, AddOrderNoteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) throw new ArgumentException("Qeyd mətni boş ola bilməz.");

        if (!await _db.Orders.AnyAsync(x => x.Id == orderId, ct))
            throw new KeyNotFoundException("Sifariş tapılmadı.");

        var note = new OrderNote
        {
            OrderId = orderId,
            AdminUserId = adminId,
            Content = request.Content.Trim()
        };

        _db.OrderNotes.Add(note);

        await _db.SaveChangesAsync(ct);

        return note.Id;
    }

    public async Task<IReadOnlyList<AdminOrderNoteDto>> GetNotesAsync(Guid orderId, CancellationToken ct)
    {
        return await _db.OrderNotes.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminOrderNoteDto(x.Id, x.AdminUserId, x.Content, x.CreatedAt))
            .ToListAsync(ct);
    }
}
