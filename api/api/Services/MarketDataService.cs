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

    public MarketDataService(
        ILogger<MarketDataService> logger,
        DataContext context,
        ICoinMarketCapService coinMarketCapService)
    {
        _logger = logger;
        _context = context;
        _coinMarketCapService = coinMarketCapService;
    }

    public async Task FetchAndProcessMarketDataAsync(CancellationToken cancellationToken)
    {
        try
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

                List<MarketDataPoint> marketDataPoints = new List<MarketDataPoint>();
                foreach (var account in user.Accounts)
                {
                    var cryptoAssetsLookup = account.CryptoAssets.ToDictionary(x => x.CoinMarketCapId, x => x.Symbol);
                    foreach (var id in allCoinIds)
                    {
                        try
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
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing coin id {CoinId} for user {UserId} account {AccountId}", id, user.Id, account.Id);
                        }
                    }
                }

                if (marketDataPoints.Any())
                {
                    try
                    {
                        await _context.MarketDataPoint.AddRangeAsync(marketDataPoints, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving market data for user {UserId}", user.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing market data");
        }
    }

}