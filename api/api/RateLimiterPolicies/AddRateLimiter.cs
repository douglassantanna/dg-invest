using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace api.RateLimiterPolicies;
public static class RateLimiterPoliciesExtensions
{
    public const string DefaultPolicy = "DefaultPolicy";
    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(policyName: DefaultPolicy, options =>
            {
                options.PermitLimit = 320;
                options.Window = TimeSpan.FromMinutes(1440);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }

}