namespace api.Services.Contracts;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendApiDownAlertAsync(string subject, string body, CancellationToken ct = default);
}
