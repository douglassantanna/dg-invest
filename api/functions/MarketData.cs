using api.Services.Contracts;
using Microsoft.Azure.Functions.Worker;
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
    }
}
