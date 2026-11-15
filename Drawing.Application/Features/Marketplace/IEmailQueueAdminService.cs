namespace Drawing.Application.Features.Marketplace;

public interface IEmailQueueAdminService
{
    Task<IReadOnlyList<FailedEmailDto>> GetFailedAsync(CancellationToken ct);
    Task RetryAsync(Guid id, CancellationToken ct);
}

public sealed record FailedEmailDto(Guid Id, string ToEmail, string Subject, int Attempts, string? LastError, DateTime CreatedAt);
