namespace api.HealthCheck;

public class DatabaseHealthCheckOptions
{
    public string FailureMessage { get; set; } = string.Empty;
    public string AlertSubject { get; set; } = string.Empty;
    public string AlertBodyPrefix { get; set; } = string.Empty;
    public bool SendAlertAlways { get; set; }
    public string AlertBodyTemplate { get; set; } = string.Empty;
}
