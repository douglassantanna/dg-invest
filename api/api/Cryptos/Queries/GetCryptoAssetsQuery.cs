using api.Cryptos.Models;
using Microsoft.Extensions.Options;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetCryptoAssetsQuery : IRequest<PageList<UserCryptoAssetDto>>
{
    public string? AssetName { get; set; } = string.Empty;
    public string? SortBy { get; set; } = "symbol";
    public string? SortOrder { get; set; } = "asc";
    public bool HideZeroBalance { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int UserId { get; set; }
    public string SubAccountTag { get; set; } = string.Empty;
}
// Not cached: the account and its assets are read from the database on every request (cheap at this scale), so the
// list is correct right after switching account, a trade or a sync. Only prices are shared, through IPriceService.
public class GetCryptoAssetsQueryHandler : IRequestHandler<GetCryptoAssetsQuery, PageList<UserCryptoAssetDto>>
{
    private const int MaxPageSize = 50;
    private readonly TransferCostPolicy _transferCostPolicy;
    private readonly DataContext _context;
    private readonly IPriceService _priceService;

    public GetCryptoAssetsQueryHandler(DataContext context,
                                        IPriceService priceService,
        IOptions<CostBasisSettings>? costBasisSettings = null)
    {
        _context = context;
        _priceService = priceService;
        _transferCostPolicy = costBasisSettings?.Value.TransferCostPolicy ?? TransferCostPolicy.Legacy;
    }

    public async Task<PageList<UserCryptoAssetDto>> Handle(GetCryptoAssetsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Account> accounts = _context.Accounts
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.AccountTransactions);
        // Transaction history is only read under Proportional (balance without history); skip it otherwise.
        accounts = _transferCostPolicy == TransferCostPolicy.Proportional
            ? accounts.Include(x => x.CryptoAssets).ThenInclude(ca => ca.Transactions)
            : accounts.Include(x => x.CryptoAssets);
        var account = await accounts
            .FirstOrDefaultAsync(x => x.IsSelected && x.UserId == request.UserId && !x.IsDeleted, cancellationToken);

        if (account == null)
            return PageList<UserCryptoAssetDto>.Empty();

        var assets = account.CryptoAssets.AsEnumerable();
        if (!string.IsNullOrEmpty(request.AssetName))
        {
            var assetName = request.AssetName.ToLower().Trim();
            assets = assets.Where(x => x.CryptoCurrency.ToLower().Contains(assetName));
        }
        var filteredAssets = assets.ToList();

        var prices = await _priceService.GetPricesAsync(filteredAssets.Select(asset => asset.CoinMarketCapId), cancellationToken);

        // The dust filter needs prices, so it runs after the price lookup (see CryptoAsset.IsVisibleWhenHidingDust).
        var cryptoAssetDtos = filteredAssets
            .Select(ca => (Asset: ca, Price: prices.PriceOf(ca.CoinMarketCapId)))
            .Where(item => !request.HideZeroBalance || item.Asset.IsVisibleWhenHidingDust(item.Price))
            .Select(item =>
            {
                var (ca, price) = item;
                var costBasis = ca.GetCostBasis(price, _transferCostPolicy);
                return new ViewMinimalCryptoAssetDto(
                    Id: ca.Id,
                    Symbol: ca.Symbol.ToLower(),
                    PricePerUnit: price,
                    Balance: ca.Balance,
                    InvestedAmount: costBasis.Invested,
                    CurrentWorth: ca.CurrentWorth(price),
                    InvestmentGainLossValue: costBasis.GainLoss,
                    InvestmentGainLossPercentage: costBasis.GainLossPercentage,
                    CoinMarketCapId: ca.CoinMarketCapId,
                    Image: ca.Symbol.ToLower());
            });

        cryptoAssetDtos = !string.IsNullOrEmpty(request.SortBy) && request.SortOrder?.ToLower() == "asc"
            ? cryptoAssetDtos.OrderBy(GetSortProperty(request))
            : cryptoAssetDtos.OrderByDescending(GetSortProperty(request));

        request.PageSize = Math.Min(request.PageSize, MaxPageSize);
        IEnumerable<UserCryptoAssetDto> result =
        [
            new UserCryptoAssetDto(account.Balance, account.Name, cryptoAssetDtos.ToList(), account.TotalDeposited(), account.Exchange == "Bybit")
        ];

        return PageList<UserCryptoAssetDto>.Create(result, request.Page, request.PageSize);
    }

    private static Func<ViewMinimalCryptoAssetDto, object> GetSortProperty(GetCryptoAssetsQuery request) =>
    request.SortBy?.ToLower() switch
    {
        "symbol" => dto => dto.Symbol,
        "balance" => dto => dto.Balance,
        "invested_amount" => dto => dto.InvestedAmount,
        _ => dto => dto.Symbol
    };
}
