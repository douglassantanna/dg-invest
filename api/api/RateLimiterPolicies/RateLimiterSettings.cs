namespace api.RateLimiterPolicies;
public class RateLimiterSettings
{
    public string RequestsPermitLimit { get; set; } = string.Empty;
    public string WindowLimitInMinutes { get; set; } = string.Empty;
}
