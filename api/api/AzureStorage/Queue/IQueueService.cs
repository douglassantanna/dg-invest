using api.Users.Models;

namespace api.AzureStorage.Queue;
public interface IQueueService
{
    Task SendWelcomeEmailAsync(User user, string subject, string body);
}
