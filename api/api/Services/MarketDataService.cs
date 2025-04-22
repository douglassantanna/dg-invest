using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Services.Contracts;
using api.Shared;
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

    public async Task<Result<bool>> FetchAndProcessMarketDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _context.Users
                .Include(x => x.Accounts)
                .ThenInclude(x => x.CryptoAssets)
                .ToListAsync(cancellationToken);

            if (!users.Any())
                return Result<bool>.Failure("No users found");

            // get all CoinMarketCapId from the account assets
            var allAssetIds = users.SelectMany(u => u.Accounts)
                       .SelectMany(a => a.CryptoAssets)
                       .Where(x => x.Balance > 0)
                       .Select(x => x.CoinMarketCapId.ToString())
                       .Distinct()
                       .ToArray();

            if (!allAssetIds.Any())
                return Result<bool>.Failure("No assets found");

            // fetch all market prices **only once**
            var coinPrices = await _coinMarketCapService.GetQuotesByIds(allAssetIds);
            if (coinPrices == null)
                return Result<bool>.Failure("Failed to fetch market prices");

            // process each user and their accounts
            var portfolioRecords = new List<UserPortfolioSnapshot>();

            foreach (var user in users)
            {
                foreach (var account in user.Accounts)
                {
                    decimal accountValue = 0;

                    // iterate the account assets to get their quantity
                    foreach (var asset in account.CryptoAssets)
                    {
                        if (asset.Balance == 0)
                            continue;

                        var assetFromCoinMarketCap = coinPrices.Data.Values.FirstOrDefault(x => x.Id == asset.CoinMarketCapId);
                        if (assetFromCoinMarketCap == null)
                            continue; // skip if no price data

                        var assetTotalValue = asset.Balance * assetFromCoinMarketCap.Quote.USD.Price;
                        accountValue = accountValue + assetTotalValue;
                    }

                    var portfolioRecord = new UserPortfolioSnapshot
                    {
                        UserId = user.Id,
                        AccountId = account.Id,
                        Value = accountValue,
                        Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    portfolioRecords.Add(portfolioRecord);
                }
            }

            if (portfolioRecords.Any())
            {
                await _context.UserPortfolioSnapshots.AddRangeAsync(portfolioRecords, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("No portfolio records to save");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing market data");
            return Result<bool>.Failure("Error fetching and processing market data");
        }
    }

}

public record UserPortfolio(int UserId, int AccountId, decimal Value, long Time);