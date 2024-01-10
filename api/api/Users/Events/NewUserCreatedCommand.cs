using api.AzureStorage.Queue;
using api.Users.Models;
using MediatR;

namespace api.Users.Events;
public record NewUserCreatedCommand(User User, string Password) : INotification;
public class NewUserCreatedCommandHandler : INotificationHandler<NewUserCreatedCommand>
{
    private readonly IQueueService _queueService;
    private readonly ILogger<NewUserCreatedCommandHandler> _logger;

    public NewUserCreatedCommandHandler(
        IQueueService queueService,
        ILogger<NewUserCreatedCommandHandler> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    public async Task Handle(NewUserCreatedCommand notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("NewUserCreatedCommandHandler: Sending welcome email to {0}", notification.User.Email);

        try
        {
            await _queueService.SendWelcomeEmailAsync(notification.User,
                                                      $"Welcome, {notification.User.FullName}",
                                                      $"This is your password. Change it ASAP: {notification.Password}");

            _logger.LogInformation("Welcome email sent.");
        }
        catch
        {
            _logger.LogError("Failed to send welcome email to {0}", notification.User.Email);
        }
    }
}