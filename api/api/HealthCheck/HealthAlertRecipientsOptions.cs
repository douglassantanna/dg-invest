namespace api.HealthCheck;

public class HealthAlertRecipientsOptions
{
    public List<HealthAlertRecipient> Recipients { get; set; } = new();
}
public class HealthAlertRecipient
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}