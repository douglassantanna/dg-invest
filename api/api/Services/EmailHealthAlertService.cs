using api.Services.Contracts;

namespace api.Services;

public class EmailHealthAlertService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailHealthAlertService> _logger;

    public EmailHealthAlertService(IEmailService emailService, ILogger<EmailHealthAlertService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async ValueTask AlertAsync(
        string subject,
        string body,
        Exception? exception = null,
        CancellationToken ct = default)
    {
        try
        {
            await _emailService.SendApiDownAlertAsync(subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send health alert: {Subject}", subject);
        }
    }
}
