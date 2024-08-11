using System.Linq.Expressions;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
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
        IQueryable<CryptoAsset> cryptoAssetQuery = _context.CryptoAssets.Include(x => x.User).ThenInclude(x => x.Account);

        int maxPageSize = 50;

        if (request.PageSize > maxPageSize)
        {
            request.PageSize = maxPageSize;
        }

        if (!string.IsNullOrEmpty(request.AssetName))
        {
            request.AssetName = request.AssetName?.ToLower().Trim();
            cryptoAssetQuery = cryptoAssetQuery.Where(x => x.CryptoCurrency.ToLower().Contains(request.AssetName));
        }

        if (request.HideZeroBalance)
        {
            cryptoAssetQuery = cryptoAssetQuery.Where(x => x.Balance > 0);
        }

        if (!string.IsNullOrEmpty(request.SortBy) && request.SortOrder?.ToLower() == "asc")
        {
            cryptoAssetQuery = cryptoAssetQuery.OrderBy(GetSortProperty(request));
        }
        else
        {
            cryptoAssetQuery = cryptoAssetQuery.OrderByDescending(GetSortProperty(request));
        }

        if (request.UserId > 0)
        {
            cryptoAssetQuery = cryptoAssetQuery.Where(x => x.UserId == request.UserId);
        }

        GetQuoteResponse cmpResponse = null;

        if (cryptoAssetQuery.Any())
        {
            cmpResponse = await GetCryptosFromcoinMarketCap(cryptoAssetQuery);
        }

        var collection = cryptoAssetQuery.Select(x => new UserCryptoAssetDto(x.User.Account.Balance,
                                                      new ViewMinimalCryptoAssetDto(x.Id,
                                                                                    x.Symbol,
                                                                                    _coinMarketCapService.GetCryptoCurrencyPriceById(x.CoinMarketCapId, cmpResponse),
                                                                                    x.Balance,
                                                                                    x.TotalInvested,
                                                                                    x.CurrentWorth(_coinMarketCapService.GetCryptoCurrencyPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.GetInvestmentGainLossValue(_coinMarketCapService.GetCryptoCurrencyPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.GetInvestmentGainLossPercentage(_coinMarketCapService.GetCryptoCurrencyPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.CoinMarketCapId)));

        var pagedCollection = await PageList<UserCryptoAssetDto>.CreateAsync(collection,
                                                                             request.Page,
                                                                             request.PageSize);

        _logger.LogInformation("ListCryptoAssetsQueryCommand. Listing crypto assets completed.");
        return pagedCollection;
    }

    private async Task<GetQuoteResponse> GetCryptosFromcoinMarketCap(IQueryable<CryptoAsset> cryptoAssetQuery)
    {
        string[] ids = cryptoAssetQuery.Select(x => x.CoinMarketCapId.ToString()).ToArray();
        return await _coinMarketCapService.GetQuotesByIds(ids);
    }

    private static Expression<Func<CryptoAsset, object>> GetSortProperty(ListCryptoAssetsQueryCommand request)
    {
        return request.SortBy?.ToLower() switch
        {
            "symbol" => currency => currency.Symbol,
            "invested_amount" => currency => currency.TotalInvested,
            // "current_worth" => currency => currency.CurrentWorth(),
            _ => currency => currency.Symbol
        };
    }
}
