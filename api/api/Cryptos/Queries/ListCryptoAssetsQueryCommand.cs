using System.Linq.Expressions;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Models;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public class ListCryptoAssetsQueryCommand : IRequest<PageList<UserCryptoAssetDto>>
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
public class ListCryptoAssetsQueryCommandHandler : IRequestHandler<ListCryptoAssetsQueryCommand, PageList<UserCryptoAssetDto>>
{
    private readonly DataContext _context;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ILogger<ListCryptoAssetsQueryCommandHandler> _logger;
    public ListCryptoAssetsQueryCommandHandler(DataContext context,
                                               ICoinMarketCapService coinMarketCapService,
                                               ILogger<ListCryptoAssetsQueryCommandHandler> logger)
    {
        _context = context;
        _coinMarketCapService = coinMarketCapService;
        _logger = logger;
    }

    public async Task<PageList<UserCryptoAssetDto>> Handle(ListCryptoAssetsQueryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ListCryptoAssetsQueryCommand. Listing crypto assets.");
        IQueryable<Account> accountQuery = _context.Accounts.Where(x => x.User.Id == request.UserId)
                                                            .Where(x => x.SubaccountTag == request.SubAccountTag);

        if (request.HideZeroBalance)
        {
            accountQuery = accountQuery.Where(x => x.Balance > 0);
        }

        if (!string.IsNullOrEmpty(request.AssetName))
        {
            var assetName = request.AssetName.ToLower().Trim();
            accountQuery = accountQuery.Include(x => x.CryptoAssets.Where(x => x.CryptoCurrency.ToLower().Contains(assetName)));
        }
        else
        {
            accountQuery = accountQuery.Include(x => x.CryptoAssets);
        }

        if (!string.IsNullOrEmpty(request.SortBy))
        {
            var sortExpression = GetSortProperty(request);
            var compiledSort = sortExpression.Compile();

            accountQuery = request.SortOrder?.ToLower() == "asc"
                ? accountQuery.OrderBy(x => compiledSort(x))
                : accountQuery.OrderByDescending(x => compiledSort(x));
        }

        var accounts = await accountQuery.ToListAsync(cancellationToken);
        GetQuoteResponse? cmpResponse = accountQuery.Any()
        ? cmpResponse = await GetCryptosFromcoinMarketCap(accountQuery)
        : null;

        var collection = accounts.SelectMany(x => x.CryptoAssets.Select(ca =>
        {
            var price = _coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse);
            return new UserCryptoAssetDto(
                ca.Id,
                new ViewMinimalCryptoAssetDto(
                    ca.Id,
                    ca.Symbol,
                    price,
                    ca.Balance,
                    ca.TotalInvested,
                    ca.CurrentWorth(price),
                    ca.GetInvestmentGainLossValue(price),
                    ca.GetInvestmentGainLossPercentage(price),
                    ca.CoinMarketCapId
                )
            );
        }));

        int maxPageSize = 50;
        if (request.PageSize > maxPageSize)
        {
            request.PageSize = maxPageSize;
        }
        var pagedCollection = await PageList<UserCryptoAssetDto>.CreateAsync(collection.AsQueryable(),
                                                                             request.Page,
                                                                             request.PageSize);

        _logger.LogInformation("ListCryptoAssetsQueryCommand. Listing crypto assets completed.");
        return pagedCollection;
    }

    private async Task<GetQuoteResponse> GetCryptosFromcoinMarketCap(IQueryable<Account> accountQuery)
    {
        var ids = accountQuery.SelectMany(x => x.CryptoAssets)
                              .Where(x => x.CoinMarketCapId > 0)
                              .Select(x => x.CoinMarketCapId.ToString())
                              .Distinct()
                              .ToArray();
        return await _coinMarketCapService.GetQuotesByIds(ids);
    }

    private static Expression<Func<Account, object>> GetSortProperty(ListCryptoAssetsQueryCommand request)
    {
        return request.SortBy?.ToLower() switch
        {
            "symbol" => account => account.CryptoAssets
                .OrderBy(x => x.Symbol)
                .Select(x => x.Symbol)
                .FirstOrDefault() ?? string.Empty,

            "invested_amount" => account => account.CryptoAssets
                .Sum(x => x.TotalInvested),

            "balance" => account => account.Balance,

            _ => account => account.Balance
        };
    }
}
