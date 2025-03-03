using System.Net;
using System.Text.Json;
using api.Services.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class MarketData
    {
        private readonly ILogger _logger;
        private readonly IMarketDataService _marketDataService;

        public MarketData(ILoggerFactory loggerFactory, IMarketDataService marketDataService)
        {
            _logger = loggerFactory.CreateLogger<MarketData>();
            _marketDataService = marketDataService;
        }

        [Function("MarketData")]
        public async Task Run([TimerTrigger("0 0 * * * *")] CancellationToken cancellationToken)
        {
            try
            {
                await _marketDataService.FetchAndProcessMarketDataAsync(cancellationToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "MarketDataFunction: error FetchAndProcessMarketDataAsync");
            }
        }
        [Function("GetDummyData")]
        public async Task<HttpResponseData> GetDummyData([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dummy")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            var dummyData = new { message = "Hello, world!", timestamp = DateTime.UtcNow };
            await response.WriteStringAsync(JsonSerializer.Serialize(dummyData));
            return response;
        }

    }
}
