using MimeKit;

namespace api.Services.Contracts;

public sealed record BybitIntegrationPausedAlert(
    string UserName,
    string UserEmail,
    int UserId,
    int? AccountId,
    string? ErrorCode,
    string ErrorMessage,
    string? Endpoint,
    int ConsecutiveTransportFailures,
    DateTime PausedAt);

public interface IEmailService
{
    Task SendMessageAsync(MimeMessage message, CancellationToken ct);
    Task SendApiDownAlertAsync(string subject, string body, CancellationToken ct = default);
    Task SendBybitIntegrationPausedAlertAsync(BybitIntegrationPausedAlert alert, CancellationToken ct = default);
}
