using System;
using api.Services.Contracts;

namespace api.Services;

public class HealthAlertService : IHealthAlertService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<HealthAlertService> _logger;

    public HealthAlertService(IEmailService emailService, ILogger<HealthAlertService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async ValueTask AlertAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            await _emailService.SendApiDownAlertAsync(subject, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to send health alert: {Subject}", subject);
        }
    }
}
