using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace api.RateLimiterPolicies;
public static class RateLimiterPoliciesExtensions
{
    public const string DefaultPolicy = "DefaultPolicy";
    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services, IConfiguration config)
    {
        RateLimiterSettings settings = new();
        config.GetSection(nameof(RateLimiterSettings)).Bind(settings);

        if (!int.TryParse(settings.RequestsPermitLimit, out var permitLimit))
        {
            permitLimit = 32;
        }

        if (!int.TryParse(settings.WindowLimitInMinutes, out var windowLimitInMinutes))
        {
            windowLimitInMinutes = 14;
        }

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(policyName: DefaultPolicy, options =>
            {
                options.PermitLimit = permitLimit;
                options.Window = TimeSpan.FromMinutes(windowLimitInMinutes);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }
}