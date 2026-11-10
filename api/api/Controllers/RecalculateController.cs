using System.Security.Claims;
using api.Cache;
using api.CoinMarketCap.Service;
using api.Data;
using api.Exchanges.Services;
using api.Models.Cryptos;
using api.Shared;
using api.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RecalculateController : ControllerBase
{
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;
    private readonly IBybitAccountSyncService _bybitAccountSyncService;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly RecalculationSettings _settings;
    private readonly TransferCostPolicy _transferCostPolicy;

    public RecalculateController(
        DataContext context,
        ICacheService cacheService,
        IBybitAccountSyncService bybitAccountSyncService,
        ICoinMarketCapService coinMarketCapService,
        IOptions<RecalculationSettings> settings,
        IOptions<CostBasisSettings> costBasisSettings)
    {
        _context = context;
        _cacheService = cacheService;
        _bybitAccountSyncService = bybitAccountSyncService;
        _coinMarketCapService = coinMarketCapService;
        _settings = settings.Value;
        _transferCostPolicy = costBasisSettings.Value.TransferCostPolicy;
    }

    // Read-only comparison of the caller's own assets: the figures shown today versus the figures after switching
    // CostBasisSettings:TransferCostPolicy to Proportional. Nothing is saved.
    [HttpGet("preview")]
    public async Task<IActionResult> Preview(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new Response("Invalid user ID", false));

        var accounts = await _context.Accounts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(account => account.UserId == userId && !account.IsDeleted)
            .Include(account => account.CryptoAssets)
                .ThenInclude(asset => asset.Transactions)
            .ToListAsync(cancellationToken);

        var assets = accounts
            .SelectMany(account => account.CryptoAssets.Select(asset => (Account: account, Asset: asset)))
            .Where(item => item.Asset.Balance > 0 || item.Asset.TotalInvested > 0)
            .ToList();
        if (assets.Count == 0)
            return Ok(new Response("ok", true, Array.Empty<CostBasisPreviewDto>()));

        var quotes = await _coinMarketCapService.GetQuotesByIds(
            assets.Select(item => item.Asset.CoinMarketCapId.ToString()).Distinct().ToArray());

        var rows = assets
            .Select(item =>
            {
                var price = _coinMarketCapService.GetCryptoCurrencyPriceById(item.Asset.CoinMarketCapId, quotes);
                return new CostBasisPreviewDto(
                    item.Account.Id,
                    item.Account.Name,
                    item.Account.AccountType.ToString(),
                    item.Asset.Id,
                    item.Asset.Symbol,
                    price,
                    item.Asset.Balance,
                    item.Asset.HistoryBalance(),
                    Before: item.Asset.GetCostBasis(price, TransferCostPolicy.Legacy),
                    After: item.Asset.PreviewCostBasis(price, TransferCostPolicy.Proportional));
            })
            .OrderByDescending(row => Math.Abs(row.After.GainLossPercentage - row.Before.GainLossPercentage))
            .ToList();

        return Ok(new Response("ok", true, rows));
    }

    // Recalculates every user's accounts, so it is restricted to admins (and still gated by EnableRecalculation).
    [HttpPost("recalculate")]
    [Authorize(Roles = nameof(Role.Admin))]
    public async Task<IActionResult> Recalculate(CancellationToken cancellationToken)
    {
        var isRecalculationEnabled = _settings.EnableRecalculation;
        if (!isRecalculationEnabled)
        {
            return BadRequest("Recalculation is not enabled");
        }

        var accounts = await _context.Accounts
            .Where(a => !a.IsDeleted)
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .ToListAsync();

        foreach (var account in accounts)
        {
            if (account.Exchange == "Bybit")
            {
                var result = await _bybitAccountSyncService.RecalculateAsync(account, cancellationToken);
                if (!result.IsSuccess && !result.IsSkipped)
                    return BadRequest(result.Message);

                if (result.IsSuccess)
                    continue;
            }

            foreach (var asset in account.CryptoAssets)
            {
                asset.RecalculateFromTransactions(_transferCostPolicy);

                // evict per-asset cache
                _cacheService.Remove($"{CacheKeyConstants.UserCryptoAsset}{asset.Id}");
            }

            // evict per-user caches
            _cacheService.Remove($"{CacheKeyConstants.UserAccountDetails}{account.UserId}");
            _cacheService.Remove(CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(account.UserId.ToString()));
        }

        await _context.SaveChangesAsync();
        return Ok("Recalculated successfully");
    }
}

public record CostBasisPreviewDto(
    int AccountId,
    string AccountName,
    string AccountType,
    int CryptoAssetId,
    string Symbol,
    decimal CurrentPrice,
    decimal Balance,
    decimal HistoryBalance,
    CostBasisView Before,
    CostBasisView After);
