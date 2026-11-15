using Drawing.Domain.Entities;
using Drawing.Domain.Enums;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.API.Infrastructure;
public sealed class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryWorker> _logger;

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
                await ProcessExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation expiry operation failed.");
            }
        }
    }

    private async Task ProcessExpiredAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DrawingDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var expired = await db.Reservations
            .Where(x => x.Status == ReservationStatus.Active && x.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(x => x.ExpiresAt)
            .Take(100)
            .ToListAsync(ct);

        foreach (var reservation in expired)
        {
            var artwork = await db.Artworks.FirstAsync(x => x.Id == reservation.ArtworkId, ct);
            reservation.Expire();

            if (artwork.SaleStatus == ArtworkSaleStatus.Reserved) artwork.ReleaseReservation();

            var order = await db.Orders.FirstAsync(x => x.Id == reservation.OrderId, ct);

            if (order.Status is OrderStatus.Confirmed or OrderStatus.AwaitingPayment)
            {
                var previous = order.Status;
                order.Cancel();

                db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    PreviousStatus = previous,
                    NewStatus = order.Status,
                    Note = "Rezervasiya müddəti başa çatdı."
                });
            }
        }

        if (expired.Count > 0) await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
