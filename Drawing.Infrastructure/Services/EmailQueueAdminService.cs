using Drawing.Application.Features.Marketplace;
using Drawing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drawing.Infrastructure.Services;

public sealed class EmailQueueAdminService : IEmailQueueAdminService
{
    private readonly DrawingDbContext _db;

    public EmailQueueAdminService(DrawingDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FailedEmailDto>> GetFailedAsync(CancellationToken ct)
    {
        return await _db.OutboxEmails.AsNoTracking()
            .Where(x => x.SentAt == null && x.Attempts >= 5)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new FailedEmailDto(x.Id, x.ToEmail, x.Subject, x.Attempts, x.LastError, x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task RetryAsync(Guid id, CancellationToken ct)
    {
        var email = await _db.OutboxEmails.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Bildiriş tapılmadı.");

        if (email.SentAt is not null) throw new InvalidOperationException("Məktub artıq göndərilib.");

        email.Attempts = 0;
        email.LastError = null;

        await _db.SaveChangesAsync(ct);
    }
}
