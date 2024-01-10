using api.Users.Models;
using Azure.Storage.Queues;

namespace api.AzureStorage.Queue;
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;

    public QueueService(
        AzureStorageSettings _storageSettings)
    {
        _queueClient = new(_storageSettings.ConnectionString, _storageSettings.WelcomeEmailQueue, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
    }

    public async Task SendWelcomeEmailAsync(User user, string subject, string body)
    {
        _queueClient.CreateIfNotExists();

        if (_queueClient.Exists())
        {
            try
            {
                var message = new SendWelcomeEmailToUser(user.Email, user.FullName, subject, body);
                await _queueClient.SendMessageAsync(message.ToString());
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}