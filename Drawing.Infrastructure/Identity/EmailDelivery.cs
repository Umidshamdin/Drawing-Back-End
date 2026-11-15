using System.Net;
using System.Net.Mail;
using System.Text;
using Drawing.Application.Abstractions.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Drawing.Infrastructure.Identity;
public sealed class EmailDelivery : IEmailDelivery
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public EmailDelivery(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SendAsync(string email, string subject, string message, CancellationToken ct = default)
    {
        var mode = _configuration["Mail:Mode"];

        if (_environment.IsDevelopment() && mode == "DevelopmentFile")
        {
            var folder = Path.Combine(_environment.ContentRootPath, "mailbox-dev");
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(path, $"To: {email}\nSubject: {subject}\n\n{message}", Encoding.UTF8, ct);

            return;
        }

        if (mode != "Smtp") throw new InvalidOperationException("Email göndərmə xidməti konfiqurasiya edilməyib.");

        var host = _configuration["Mail:Host"];
        var from = _configuration["Mail:From"];
        var username = _configuration["Mail:Username"];
        var password = _configuration["Mail:Password"];
        var port = _configuration.GetValue<int>("Mail:Port");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || port <= 0)
            throw new InvalidOperationException("SMTP konfiqurasiyası natamamdır.");

        using var mail = new MailMessage(from, email, subject, message) { IsBodyHtml = false };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(mail, ct);
    }
}