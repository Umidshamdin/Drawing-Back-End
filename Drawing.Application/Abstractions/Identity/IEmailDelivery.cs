namespace Drawing.Application.Abstractions.Identity;
public interface IEmailDelivery
{
    Task SendAsync(string email, string subject, string message, CancellationToken ct = default);
}
