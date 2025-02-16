
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Services;
public class MarketDataBackgroundService : BackgroundService
{
    private readonly TimeSpan _fetchInterval = TimeSpan.FromMinutes(60);
    private readonly ILogger<MarketDataBackgroundService> _logger;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MarketDataBackgroundService(
        ILogger<MarketDataBackgroundService> logger,
        ICoinMarketCapService coinMarketCapService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _coinMarketCapService = coinMarketCapService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            {
                // create a new scope to get the dbContext
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                // get all users and their accounts
                var users = await dbContext.Users
                .Include(x => x.Accounts)
                .ThenInclude(x => x.CryptoAssets)
                .ToListAsync(cancellationToken: stoppingToken);

                foreach (var user in users)
                {
                    // get all the coinMarketCapIds for the user's accounts(portfolios)
                    foreach (var account in user.Accounts)
                    {
                        // get the coinMarketCapId for each crypto asset in the account
                        var uniqueCoinIds = account.CryptoAssets.Select(x => x.CoinMarketCapId.ToString()).Distinct().ToArray();
                        GetQuoteResponse? marketData = await _coinMarketCapService.GetQuotesByIds(uniqueCoinIds);

                        List<MarketDataPoint> marketDataPoints = [];
                        // create a new marketDataPoint for each coinMarketCapId(user's asset)

                        var cryptoAssetsLookup = account.CryptoAssets.ToDictionary(x => x.CoinMarketCapId, x => x.Symbol);
                        foreach (var id in uniqueCoinIds)
                        {
                            var parsedId = int.Parse(id);
                            var symbol = cryptoAssetsLookup.TryGetValue(parsedId, out var coinSymbol) ? coinSymbol : "";
                            var price = _coinMarketCapService.GetCryptoCurrencyPriceById(parsedId, marketData);
                            marketDataPoints.Add(new MarketDataPoint
                            (
                                user.Id,
                                account.Id,
                                symbol,
                                price,
                                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            ));
                        }

                        if (marketDataPoints.Any())
                        {
                            await dbContext.MarketData.AddRangeAsync(marketDataPoints, stoppingToken);
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
            }
            await Task.Delay(_fetchInterval, stoppingToken);
        }
    }
}