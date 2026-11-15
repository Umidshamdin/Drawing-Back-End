namespace Drawing.Infrastructure.Notifications;

public sealed class OutboxEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
