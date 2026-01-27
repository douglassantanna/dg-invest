namespace api.HealthCheck;

public class HealthPingOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string FunctionKey { get; set; } = string.Empty;
    public bool RunFunction { get; set; }
}
