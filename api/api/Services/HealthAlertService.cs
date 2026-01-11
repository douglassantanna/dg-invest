using System;
using api.Services.Contracts;

namespace api.Services;

public class HealthAlertService : IHealthAlertService
{
    private readonly IEmailService _emailService;

    public HealthAlertService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public ValueTask AlertAsync(string subject, string body, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
