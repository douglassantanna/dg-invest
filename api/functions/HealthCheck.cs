using api.Services.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class HealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthCheck> _logger;

        public HealthCheck(IServiceProvider serviceProvider, ILogger<HealthCheck> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("DatabaseKeepAlive")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, FunctionContext context)
        {
            _logger.LogInformation("Database keep-alive triggered at: {time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();

            try
            {
                var result = await healthCheckService.IsDatabaseHealthyAsync(context.CancellationToken);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Database health check succeeded.");
                }
                else
                {
                    _logger.LogError("Database health check failed: {error}", result.Error);
                    throw new InvalidOperationException($"Database health check failed: {result.Error}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Database health check failed: {message}", ex.Message);
                throw;
            }
        }
    }
}