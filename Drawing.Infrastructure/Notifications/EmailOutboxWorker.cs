using Drawing.Application.Abstractions.Identity;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drawing.API.Infrastructure;

public sealed class EmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxWorker> _logger;

    public EmailOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;

                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email outbox processing failed.");
            }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DrawingDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailDelivery>();

        var messages = await db.OutboxEmails
            .Where(x => x.SentAt == null && x.Attempts < 5)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await email.SendAsync(message.ToEmail, message.Subject, message.Body, ct);
                message.SentAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.GetType().Name;
                _logger.LogWarning("Email sending attempt failed. MessageId: {MessageId}", message.Id);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
