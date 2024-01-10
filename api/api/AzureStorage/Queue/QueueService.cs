using api.Users.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;

namespace api.AzureStorage.Queue;
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly AzureStorageSettings _storageSettings;
    public QueueService(IOptions<AzureStorageSettings> storageSettings)
    {
        _storageSettings = storageSettings.Value;
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
            catch (System.Exception ex)
            {
                throw;
            }
        }
    }
}