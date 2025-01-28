using api.Cache;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Repositories;
using api.Shared;
using Flurl.Http;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetCryptoAssetByIdQuery(int CryptoAssetId) : IRequest<Response>;
public class GetCryptoAssetByIdQueryHandler : IRequestHandler<GetCryptoAssetByIdQuery, Response>
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ILogger<GetCryptoAssetByIdQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetCryptoAssetByIdQueryHandler(
        ICoinMarketCapService coinMarketCapService,
        ICryptoAssetRepository cryptoAssetRepository,
        ILogger<GetCryptoAssetByIdQueryHandler> logger,
        ICacheService cacheService)
    {
        _coinMarketCapService = coinMarketCapService;
        _cryptoAssetRepository = cryptoAssetRepository;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyConstants.CryptoAsset}{request.CryptoAssetId}";
        var absoluteExpiration = TimeSpan.FromMinutes(1);
        var cryptoInfo = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
        {
            var cryptoAsset = await _cryptoAssetRepository.GetByIdAsync(request.CryptoAssetId,
                                                                        x => x.Include(q => q.Transactions));
            if (cryptoAsset is null)
            {
                _logger.LogError("GetCryptoAssetByIdQuery. CryptoAssetId: {0} not found", request.CryptoAssetId);
                return null;
            }

            GetQuoteResponse cmpResponse;
            try
            {
                cmpResponse = await _coinMarketCapService.GetQuotesByIds(new[] { cryptoAsset.CoinMarketCapId.ToString() });
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogError(ex, "GetCryptoAssetByIdQuery. Error getting quotes for CryptoAssetId: {0}", request.CryptoAssetId);
                return null;
            }

            var currentPrice = _coinMarketCapService.GetCryptoCurrencyPriceById(cryptoAsset.CoinMarketCapId, cmpResponse);
            List<CryptoAssetData> cards = new()
            {
                new CryptoAssetData("Current price", currentPrice),
                new CryptoAssetData("Average price", cryptoAsset.AveragePrice),
                new CryptoAssetData("Balance", cryptoAsset.Balance),
                new CryptoAssetData("Invested amount", cryptoAsset.TotalInvested),
                new CryptoAssetData("Current worth", cryptoAsset.CurrentWorth(currentPrice)),
                new CryptoAssetData("Gain/Loss", cryptoAsset.GetInvestmentGainLossValue(currentPrice), cryptoAsset.GetInvestmentGainLossPercentage(currentPrice)),
            };

            return new ViewCryptoAssetDto
            (
                cryptoAsset.Id,
                new ViewCryptoInformation
                (
                    cryptoAsset.Symbol.ToLower(),
                    cryptoAsset.CoinMarketCapId
                ),
                cards,
                cryptoAsset.Transactions.Select(t => new ViewCryptoTransactionDto(t.Id,
                                                                                t.Amount,
                                                                                t.Price,
                                                                                t.PurchaseDate,
                                                                                t.ExchangeName,
                                                                                t.TransactionType,
                                                                                t.GetPercentDifference(currentPrice),
                                                                                t.Fee)).ToList(),
                cryptoAsset.Addresses.Select(a => new ViewAddressDto(a.Id,
                                                                    a.AddressName,
                                                                    a.AddressValue)).ToList()
            );
        },
        absoluteExpiration: absoluteExpiration,
        cancellationToken: cancellationToken);

        if (cryptoInfo is null)
        {
            return new Response("Error getting crypto asset", false);
        }
        return new Response("", true, cryptoInfo);
    }
}
