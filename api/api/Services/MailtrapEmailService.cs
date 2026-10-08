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
        IOptions<HealthAlertRecipientsOptions> recipientsOptions)
    {
        _settings = settings.Value;
        _logger = logger;
        _recipientsOptions = recipientsOptions.Value;
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

        if (string.IsNullOrWhiteSpace(_settings.From))
        {
            _logger.LogError("Mailtrap From address is not configured.");
            return;
        }

        try
        {
            var message = BuildMessage(recipients, subject, body);
            await SendMessageAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email via Mailtrap. Subject: {Subject}", subject);
            // important: do NOT throw here (we don't want health check to fail just because alerting failed)
        }
    }

    public async Task SendBybitIntegrationPausedAlertAsync(BybitIntegrationPausedAlert alert, CancellationToken ct = default)
    {
        var recipients = _recipientsOptions.Recipients
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email))
            .Select(recipient => new MailboxAddress(recipient.Name, recipient.Email))
            .Append(string.IsNullOrWhiteSpace(alert.UserEmail)
                ? null
                : new MailboxAddress(alert.UserName, alert.UserEmail))
            .Where(recipient => recipient is not null)
            .Select(recipient => recipient!)
            .GroupBy(recipient => recipient.Address, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (recipients.Count == 0)
            throw new InvalidOperationException("No user or support recipients are configured for the Bybit pause alert.");
        if (string.IsNullOrWhiteSpace(_settings.From))
            throw new InvalidOperationException("Mailtrap From address is not configured.");

        var subject = $"Bybit integration paused for {alert.UserName}";
        var failureDescription = string.Equals(alert.ErrorCode, "transport", StringComparison.OrdinalIgnoreCase)
            ? $"Consecutive transport failures: {alert.ConsecutiveTransportFailures}"
            : "A permanent Bybit credential/API error was returned.";
        var body = $"""
            The Bybit integration for {alert.UserName} (user ID {alert.UserId}) has been automatically paused.

            Account ID: {alert.AccountId?.ToString() ?? "integration-level"}
            Paused at (UTC): {alert.PausedAt:O}
            Error code: {alert.ErrorCode ?? "not available"}
            Endpoint: {alert.Endpoint ?? "not available"}
            {failureDescription}
            Error: {alert.ErrorMessage}

            Review the account credentials, Bybit API permissions, IP restrictions, region, and recent service/network status. After the cause is corrected, test the connection and explicitly resume the integration in DG Invest.
            """;

        await SendMessageAsync(BuildMessage(recipients, subject, body), ct);
    }

    public async Task SendMessageAsync(MimeMessage message, CancellationToken ct)
    {
        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);
    }

    private MimeMessage BuildMessage(IEnumerable<MailboxAddress> recipients, string subject, string body)
    {
        var message = new MimeMessage();

        message.From.Add(MailboxAddress.Parse(_settings.From));

        foreach (var recipient in recipients)
            message.To.Add(recipient);

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = body,
            HtmlBody = $"<pre style='font-family: monospace; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(body)}</pre>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
