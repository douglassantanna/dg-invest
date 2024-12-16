using System.Linq.Expressions;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Models;
using api.Data;
using api.Models.Cryptos;
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
        var filteredByTag = _context.Accounts.Where(x => x.SubaccountTag == "main");
        var filteredByUser = filteredByTag.Where(x => x.UserId == request.UserId);

        if (request.HideZeroBalance)
        {
            filteredByUser = filteredByUser.Where(x => x.Balance > 0);
        }

        if (!string.IsNullOrEmpty(request.AssetName))
        {
            var assetName = request.AssetName.ToLower().Trim();
            filteredByUser = filteredByUser.Include(x => x.CryptoAssets.Where(x => x.CryptoCurrency.ToLower().Contains(assetName)));
        }
        else
        {
            filteredByUser = filteredByUser.Include(x => x.CryptoAssets);
        }


        if (!string.IsNullOrEmpty(request.SortBy) && request.SortOrder?.ToLower() == "asc")
        {
            filteredByUser = filteredByUser.OrderBy(GetSortProperty(request));
        }
        else
        {
            filteredByUser = filteredByUser.OrderByDescending(GetSortProperty(request));
        }

        var accounts = await filteredByUser.ToListAsync(cancellationToken);
        GetQuoteResponse cmpResponse = null;

        var cryptoAssets = accounts.SelectMany(x => x.CryptoAssets).ToList();
        if (cryptoAssets.Any())
        {
            cmpResponse = await GetCryptosFromcoinMarketCap(cryptoAssets);
        }

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

    private async Task<GetQuoteResponse> GetCryptosFromcoinMarketCap(List<CryptoAsset> accountQuery)
    {
        string[] ids = accountQuery.Select(x => x.CoinMarketCapId.ToString()).ToArray();
        return await _coinMarketCapService.GetQuotesByIds(ids);
    }

    private static Expression<Func<Account, object>> GetSortProperty(ListCryptoAssetsQueryCommand request)
    {
        return request.SortBy?.ToLower() switch
        {
            "symbol" => account => account.CryptoAssets.FirstOrDefault().Symbol ?? string.Empty,
            "invested_amount" => account => account.CryptoAssets.FirstOrDefault().TotalInvested!,
            "balance" => account => account.CryptoAssets.FirstOrDefault().CurrentWorth,
            _ => account => account.CryptoAssets.FirstOrDefault().Symbol ?? string.Empty
        };
    }
}
