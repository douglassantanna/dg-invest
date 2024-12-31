using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using Flurl.Http;
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

        var account = await _context.Accounts
            .Include(x => x.CryptoAssets)
            .FirstOrDefaultAsync(x => x.IsSelected && x.UserId == request.UserId, cancellationToken);

        if (account == null)
        {
            return PageList<UserCryptoAssetDto>.Empty();
        }

        var cryptoAssets = account.CryptoAssets.AsQueryable();

        if (request.HideZeroBalance)
        {
            cryptoAssets = cryptoAssets.Where(x => x.Balance > 0);
        }

        if (!string.IsNullOrEmpty(request.AssetName))
        {
            var assetName = request.AssetName.ToLower().Trim();
            cryptoAssets = cryptoAssets.Where(x => x.CryptoCurrency.ToLower().Contains(assetName));
        }

        var filteredAssets = cryptoAssets.ToList();

        GetQuoteResponse? cmpResponse = null;
        if (cryptoAssets.Any())
        {
            try
            {
                cmpResponse = await GetCryptosFromcoinMarketCap(cryptoAssets.ToList());
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogError(ex, "Error getting quotes from CoinMarketCap");
            }
        }

        var cryptoAssetDtos = filteredAssets.Select(ca =>
        new ViewMinimalCryptoAssetDto(
            Id: ca.Id,
            Symbol: ca.Symbol,
            PricePerUnit: _coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse),
            Balance: ca.Balance,
            InvestedAmount: ca.TotalInvested,
            CurrentWorth: ca.CurrentWorth(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
            InvestmentGainLossValue: ca.GetInvestmentGainLossValue(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
            InvestmentGainLossPercentage: ca.GetInvestmentGainLossPercentage(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
            CoinMarketCapId: ca.CoinMarketCapId
        ));


        if (!string.IsNullOrEmpty(request.SortBy) && request.SortOrder?.ToLower() == "asc")
        {
            cryptoAssetDtos = cryptoAssetDtos.OrderBy(GetSortProperty(request));
        }
        else
        {
            cryptoAssetDtos = cryptoAssetDtos.OrderByDescending(GetSortProperty(request));
        }

        int maxPageSize = 50;
        request.PageSize = Math.Min(request.PageSize, maxPageSize);

        IEnumerable<UserCryptoAssetDto> result;

        if (!cryptoAssetDtos.Any())
        {
            result = [new UserCryptoAssetDto(account.Balance, account.SubaccountTag, [])];
        }
        else
        {
            result =
            [
                new UserCryptoAssetDto
                (
                    account.Balance,
                    account.SubaccountTag,
                    cryptoAssetDtos
                )
            ];
        }

        return PageList<UserCryptoAssetDto>.Create(result, request.Page, request.PageSize);
    }

    private async Task<GetQuoteResponse> GetCryptosFromcoinMarketCap(List<CryptoAsset> accountQuery)
    {
        string[] ids = accountQuery.Select(x => x.CoinMarketCapId.ToString()).ToArray();
        return await _coinMarketCapService.GetQuotesByIds(ids);
    }

    private static Func<ViewMinimalCryptoAssetDto, object> GetSortProperty(ListCryptoAssetsQueryCommand request) =>
    request.SortBy?.ToLower() switch
    {
        "symbol" => dto => dto.Symbol,
        "balance" => dto => dto.Balance,
        "invested_amount" => dto => dto.InvestedAmount,
        _ => dto => dto.Symbol
    };
}
