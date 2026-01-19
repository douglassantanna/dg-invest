using api.Email;
using api.Services.Contracts;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using api.HealthCheck;

namespace api.Services;

public class MailtrapEmailService : IEmailService
{
    private readonly MailtrapSettings _settings;
    private readonly ILogger<MailtrapEmailService> _logger;
    private readonly HealthAlertRecipientsOptions _recipientsOptions;

    public MailtrapEmailService(
        IOptions<MailtrapSettings> settings,
        ILogger<MailtrapEmailService> logger,
        HealthAlertRecipientsOptions recipientsOptions)
    {
        _settings = settings.Value;
        _logger = logger;
        _recipientsOptions = recipientsOptions;
    }

    public async Task SendApiDownAlertAsync(string subject, string body, CancellationToken ct = default)
    {
        var recipients = _recipientsOptions.Recipients
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email))
            .Select(recipient => new MailboxAddress(recipient.Name, recipient.Email))
            .ToList();

        if (recipients.Count == 0)
        {
            _logger.LogError("No recipients configured for health alert emails.");
            return;
        }

        var message = BuildMessage(recipients, subject, body);

        try
        {
            await SendMessageAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email via Mailtrap. Subject: {Subject}", subject);
            // important: do NOT throw here (we don't want health check to fail just because alerting failed)
        }
    }

    public async Task SendMessageAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private MimeMessage BuildMessage(IEnumerable<MailboxAddress> recipients, string subject, string body)
    {
        var message = new MimeMessage();

        foreach (var recipient in recipients)
            message.To.Add(recipient);

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = body,
            HtmlBody = $"<pre style='font-family: monospace; white-space: pre-wrap;'>{body}</pre>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
