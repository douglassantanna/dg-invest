using api.Email;
using api.Services.Contracts;
using Microsoft.Extensions.Options;

namespace api.Services;

public class MailtrapEmailService : IEmailService
{
    private readonly MailtrapSettings _mailtrapSettings;

    public MailtrapEmailService(IOptions<MailtrapSettings> mailtrapSettings)
    {
        _mailtrapSettings = mailtrapSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
      await Task.Delay(1);
    }
}
