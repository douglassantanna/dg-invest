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
            .FirstOrDefaultAsync(x => x.SubaccountTag == "main" && x.UserId == request.UserId, cancellationToken);

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
            cmpResponse = await GetCryptosFromcoinMarketCap(cryptoAssets.ToList());
        }

        var dtos = filteredAssets.Select(ca =>
        new UserCryptoAssetDto(
            account.Balance,
            new ViewMinimalCryptoAssetDto(
                ca.Id,
                ca.Symbol,
                _coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse),
                ca.Balance,
                ca.TotalInvested,
                ca.CurrentWorth(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
                ca.GetInvestmentGainLossValue(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
                ca.GetInvestmentGainLossPercentage(_coinMarketCapService.GetCryptoCurrencyPriceById(ca.CoinMarketCapId, cmpResponse)),
                ca.CoinMarketCapId,
                ca.TotalInvested
            )
        ));

        if (!string.IsNullOrEmpty(request.SortBy) && request.SortOrder?.ToLower() == "asc")
        {
            dtos = dtos.OrderBy(GetSortProperty(request));
        }
        else
        {
            dtos = dtos.OrderByDescending(GetSortProperty(request));
        }

        int maxPageSize = 50;
        request.PageSize = Math.Min(request.PageSize, maxPageSize);

        return PageList<UserCryptoAssetDto>.Create(dtos, request.Page, request.PageSize);
    }

    private async Task<GetQuoteResponse> GetCryptosFromcoinMarketCap(List<CryptoAsset> accountQuery)
    {
        string[] ids = accountQuery.Select(x => x.CoinMarketCapId.ToString()).ToArray();
        return await _coinMarketCapService.GetQuotesByIds(ids);
    }

    private static Func<UserCryptoAssetDto, object> GetSortProperty(ListCryptoAssetsQueryCommand request) =>
    request.SortBy?.ToLower() switch
    {
        "symbol" => dto => dto.CryptoAssetDto.Symbol,
        "balance" => dto => dto.CryptoAssetDto.Balance,
        "invested_amount" => dto => dto.CryptoAssetDto.TotalInvested,
        _ => dto => dto.CryptoAssetDto.Symbol
    };
}
