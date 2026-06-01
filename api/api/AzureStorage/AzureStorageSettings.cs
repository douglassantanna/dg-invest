namespace api.AzureStorage;
public class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string WelcomeEmailQueue { get; set; } = string.Empty;
}
public record SendWelcomeEmailToUser(string Email, string Name, string Subject, string Body);