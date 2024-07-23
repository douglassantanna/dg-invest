using api.Users.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;

namespace api.AzureStorage.Queue;
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly AzureStorageSettings _storageSettings;
    private readonly ILogger<QueueService> _logger;

    public QueueService(IOptions<AzureStorageSettings> storageSettings, ILogger<QueueService> logger)
    {
        _storageSettings = storageSettings.Value;
        _queueClient = new(_storageSettings.ConnectionString, _storageSettings.WelcomeEmailQueue, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(User user, string subject, string body)
    {
        _logger.LogInformation("Sending welcome email to user: {Email}", user.Email);

        _queueClient.CreateIfNotExists();

        if (_queueClient.Exists())
        {
            try
            {
                var message = new SendWelcomeEmailToUser(user.Email, user.FullName, subject, body);
                await _queueClient.SendMessageAsync(message.ToString());
                _logger.LogInformation("Welcome email message sent to queue for user: {Email}", user.Email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email message to queue for user: {Email}", user.Email);
                throw;
            }
        }
        else
        {
            _logger.LogError("Queue does not exist: {QueueName}", _storageSettings.WelcomeEmailQueue);
        }
    }
}
