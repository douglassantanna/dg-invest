using api.Users.Models;
using Azure.Storage.Queues;

namespace api.AzureStorage.Queue;
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        AzureStorageSettings _storageSettings,
        ILogger<QueueService> logger)
    {
        _queueClient = new(_storageSettings.ConnectionString, _storageSettings.QueueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(User user, string subject, string body)
    {
        _queueClient.CreateIfNotExists();

        if (_queueClient.Exists())
        {
            try
            {
                _logger.LogInformation("Sending welcome email to {email}", user.Email);

                var message = new SendWelcomeEmailToUser(user.Email, user.FullName, "Welcome", "Welcome email");
                await _queueClient.SendMessageAsync(message.ToString());

                _logger.LogInformation("Welcome email sent to {email}", user.Email);
            }
            catch (System.Exception)
            {
                _logger.LogError("Failed to send welcome email to {email}", user.Email);
                _logger.LogError("Error: {error}", System.Environment.StackTrace);
                throw;
            }
        }
    }
}