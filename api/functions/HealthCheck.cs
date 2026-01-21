using api.HealthCheck;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace functions
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly HttpClient _httpClient;
        private readonly HealthPingOptions _options;
        private const string FunctionKeyHeaderName = "X-Function-Key";

        public HealthCheck(ILogger<HealthCheck> logger,
                           HttpClient httpClient,
                           IOptions<HealthPingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options.Value;

            // Set reasonable timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        [Function("DatabaseKeepAlive")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer, FunctionContext context)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                _logger.LogError("Health ping endpoint is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.FunctionKey))
            {
                _logger.LogError("Health ping function key is not configured.");
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _options.Endpoint);
                request.Headers.Add(FunctionKeyHeaderName, _options.FunctionKey);

                using var response = await _httpClient.SendAsync(request, context.CancellationToken);
                if (!response.IsSuccessStatusCode)
                    _logger.LogError("Health check returned {StatusCode}", (int)response.StatusCode);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Health check was cancelled.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Health check request failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with unexpected error.");
            }
        }
    }
}
