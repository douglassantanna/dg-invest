
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Services;
public class MarketDataBackgroundService : BackgroundService
{
    private readonly TimeSpan _fetchInterval = TimeSpan.FromMinutes(3);
    private readonly ILogger<MarketDataBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MarketDataBackgroundService(
        ILogger<MarketDataBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(_fetchInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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

                    if (!users.Any())
                    {
                        _logger.LogError("No users found. Skipping this execution cycle.");
                        await Task.Delay(_fetchInterval, stoppingToken);
                        continue;
                    }

                    foreach (var user in users)
                    {
                        // get all the coinMarketCapIds for the user's accounts(portfolios)
                        var allCoinIds = user.Accounts
                            .SelectMany(x => x.CryptoAssets)
                            .Select(x => x.CoinMarketCapId.ToString())
                            .Distinct()
                            .ToArray();

                        // use 'continue' to skip the user with no crypto assets
                        if (!allCoinIds.Any())
                            continue;

                        var coinMarketCapService = scope.ServiceProvider.GetRequiredService<ICoinMarketCapService>();
                        GetQuoteResponse? marketData = await coinMarketCapService.GetQuotesByIds(allCoinIds);

                        List<MarketDataPoint> marketDataPoints = [];
                        foreach (var account in user.Accounts)
                        {
                            // create a new marketDataPoint for each coinMarketCapId(user's asset)
                            var cryptoAssetsLookup = account.CryptoAssets.ToDictionary(x => x.CoinMarketCapId, x => x.Symbol);
                            foreach (var id in allCoinIds)
                            {
                                var parsedId = int.Parse(id);
                                var symbol = cryptoAssetsLookup.TryGetValue(parsedId, out var coinSymbol) ? coinSymbol : "";
                                var price = coinMarketCapService.GetCryptoCurrencyPriceById(parsedId, marketData);
                                marketDataPoints.Add(new MarketDataPoint
                                (
                                    user.Id,
                                    account.Id,
                                    symbol,
                                    price,
                                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                ));
                            }
                        }
                        if (marketDataPoints.Any())
                        {
                            // using raw SQL for efficient batch upsert, reducing duplicate checks and DB round trips
                            var values = string.Join(",", marketDataPoints.Select(m =>
                                $"({m.UserId}, {m.AccountId}, '{m.CoinSymbol}', {m.CoinPrice}, {m.Time})"));

                            var sql = $@"
                                MERGE INTO MarketDataPoint AS Target
                                USING (VALUES {values}) 
                                AS Source (UserId, AccountId, CoinSymbol, CoinPrice, Time)
                                ON Target.UserId = Source.UserId 
                                AND Target.AccountId = Source.AccountId 
                                AND Target.CoinSymbol = Source.CoinSymbol 
                                AND Target.Time = Source.Time
                                WHEN MATCHED THEN 
                                    UPDATE SET Target.CoinPrice = Source.CoinPrice
                                WHEN NOT MATCHED THEN 
                                    INSERT (UserId, AccountId, CoinSymbol, CoinPrice, Time)
                                    VALUES (Source.UserId, Source.AccountId, Source.CoinSymbol, Source.CoinPrice, Source.Time);";
                            await dbContext.Database.ExecuteSqlRawAsync(sql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching market data.");
            }
            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }
}