using MimeKit;

namespace api.Services.Contracts;

public interface IEmailService
{
    Task SendMessageAsync(MimeMessage message, CancellationToken ct);
    Task SendApiDownAlertAsync(string subject, string body, CancellationToken ct = default);
}
