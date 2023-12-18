namespace api.RateLimiterPolicies;
public class RateLimiterSettings
{
    public int RequestsPermitLimit { get; set; }
    public int WindowLimitInMinutes { get; set; }
}
