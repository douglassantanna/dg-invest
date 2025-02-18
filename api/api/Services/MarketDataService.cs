using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace api.Services;
public class MarketDataService : IMarketDataService
{
    private readonly ILogger<MarketDataService> _logger;
    private readonly DataContext _context;
    private readonly ICoinMarketCapService _coinMarketCapService;

    public MarketDataService(ILogger<MarketDataService> logger, DataContext context, ICoinMarketCapService coinMarketCapService)
    {
        _logger = logger;
        _context = context;
        _coinMarketCapService = coinMarketCapService;
    }

    public async Task FetchAndProcessMarketDataAsync(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Include(x => x.Accounts)
            .ThenInclude(x => x.CryptoAssets)
            .ToListAsync(cancellationToken);

        if (!users.Any())
            return;

        foreach (var user in users)
        {
            var allCoinIds = user.Accounts
                .SelectMany(x => x.CryptoAssets)
                .Select(x => x.CoinMarketCapId.ToString())
                .Distinct()
                .ToArray();

            if (!allCoinIds.Any())
                continue;

            var marketData = await _coinMarketCapService.GetQuotesByIds(allCoinIds);

            List<MarketDataPoint> marketDataPoints = [];
            foreach (var account in user.Accounts)
            {
                var cryptoAssetsLookup = account.CryptoAssets.ToDictionary(x => x.CoinMarketCapId, x => x.Symbol);
                foreach (var id in allCoinIds)
                {
                    var parsedId = int.Parse(id);
                    var symbol = cryptoAssetsLookup.TryGetValue(parsedId, out var coinSymbol) ? coinSymbol : "";
                    var price = _coinMarketCapService.GetCryptoCurrencyPriceById(parsedId, marketData);

                    if (!string.IsNullOrEmpty(symbol))
                    {
                        marketDataPoints.Add(new MarketDataPoint
                        (
                            user.Id, account.Id, symbol, price, DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        ));
                    }
                }
            }

            if (marketDataPoints.Any())
            {
                _context.MarketDataPoint.AddRangeAsync(marketDataPoints);
                await _context.SaveChangesAsync();
            }
        }
    }
    private async Task SaveMarketDataPoints(List<MarketDataPoint> marketDataPoints)
    {
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

        await _context.Database.ExecuteSqlRawAsync(sql);
    }
}