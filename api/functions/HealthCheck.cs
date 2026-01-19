using api.HealthCheck;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace functions
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HealthPingOptions _options;

        public HealthCheck(IServiceProvider serviceProvider,
                           ILogger<HealthCheck> logger,
                           IHttpClientFactory httpClientFactory,
                           IOptions<HealthPingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        [Function("DatabaseKeepAlive")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, FunctionContext context)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                _logger.LogError("Health ping endpoint is not configured.");
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(_options.Endpoint, context.CancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Database health check succeeded.");
                }
                else
                {
                    _logger.LogError("Database health check failed. StatusCode: {StatusCode}", (int)response.StatusCode);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Database health check failed: {message}", ex.Message);
            }
        }
    }
}