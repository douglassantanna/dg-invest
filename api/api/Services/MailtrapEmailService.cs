using api.Email;
using api.Services.Contracts;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;

namespace api.Services;

public class MailtrapEmailService : IEmailService
{
    private readonly MailtrapSettings _settings;
    private readonly ILogger<MailtrapEmailService> _logger;

    public MailtrapEmailService(
        IOptions<MailtrapSettings> settings,
        ILogger<MailtrapEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendApiDownAlertAsync(string subject, string body, CancellationToken ct = default)
    {
        Dictionary<string, string> usersToNotify = new()
        {
            {"Douglas Sant'anna", "douglbb1@gmail.com"}
        };
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.Username, _settings.From));

        foreach (var user in usersToNotify)
        {
            message.To.Add(new MailboxAddress(user.Key, user.Value));
        }

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = body,
            HtmlBody = $"<pre style='font-family: monospace; white-space: pre-wrap;'>{body}</pre>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            // For Mailtrap sandbox - disable certificate validation
            // (DO NOT do this in production with real SMTP!)
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

            await client.SendAsync(message, ct);

            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Alert email sent successfully via Mailtrap. Subject: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email via Mailtrap. Subject: {Subject}", subject);
            // Important: do NOT throw here — we don't want health check to fail just because alerting failed
        }
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        throw new NotImplementedException();
    }
}
