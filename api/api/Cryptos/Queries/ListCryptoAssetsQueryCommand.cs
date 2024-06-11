using System.Linq.Expressions;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using MediatR;

namespace api.Cryptos.Queries;
public class ListCryptoAssetsQueryCommand : IRequest<PageList<ViewMinimalCryptoAssetDto>>
{
    public string? AssetName { get; set; } = string.Empty;
    public string? SortBy { get; set; } = "symbol";
    public string? SortOrder { get; set; } = "asc";
    public bool HideZeroBalance { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int UserId { get; set; }
}
public class ListCryptoAssetsQueryCommandHandler : IRequestHandler<ListCryptoAssetsQueryCommand, PageList<ViewMinimalCryptoAssetDto>>
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

    public async Task<PageList<ViewMinimalCryptoAssetDto>> Handle(ListCryptoAssetsQueryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ListCryptoAssetsQueryCommand. Listing crypto assets.");
        IQueryable<CryptoAsset> cryptoAssetQuery = _context.CryptoAssets;

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

        var collection = cryptoAssetQuery.Select(x => new ViewMinimalCryptoAssetDto(x.Id,
                                                                                    x.Symbol,
                                                                                    GetCryptoCurrentPriceById(x.CoinMarketCapId, cmpResponse),
                                                                                    x.Balance,
                                                                                    x.TotalInvested,
                                                                                    x.CurrentWorth(GetCryptoCurrentPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.GetInvestmentGainLossValue(GetCryptoCurrentPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.GetInvestmentGainLossPercentage(GetCryptoCurrentPriceById(x.CoinMarketCapId, cmpResponse)),
                                                                                    x.CoinMarketCapId));

        var pagedCollection = await PageList<ViewMinimalCryptoAssetDto>.CreateAsync(collection,
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

    private static decimal GetCryptoCurrentPriceById(int coinMarketCapId, GetQuoteResponse? cmpResponse)
    {
        if (cmpResponse != null)
        {
            var coin = cmpResponse.Data.FirstOrDefault(coin => coin.Key.ToString() == coinMarketCapId.ToString());
            if (coin.Value != null)
            {
                return coin.Value.Quote.USD.Price;
            }
        }
        return 0;
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
