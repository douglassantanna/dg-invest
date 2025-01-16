using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Repositories;
using api.Shared;
using Flurl.Http;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetCryptoAssetByIdCommandQuery(int CryptoAssetId) : IRequest<Response>;
public class GetCryptoAssetByIdCommandQueryHandler : IRequestHandler<GetCryptoAssetByIdCommandQuery, Response>
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ILogger<GetCryptoAssetByIdCommandQueryHandler> _logger;

    public GetCryptoAssetByIdCommandQueryHandler(
        ICoinMarketCapService coinMarketCapService,
        ICryptoAssetRepository cryptoAssetRepository,
        ILogger<GetCryptoAssetByIdCommandQueryHandler> logger)
    {
        _coinMarketCapService = coinMarketCapService;
        _cryptoAssetRepository = cryptoAssetRepository;
        _logger = logger;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdCommandQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCryptoAssetByIdCommandQuery. Retrieving CryptoAssetId: {0}", request.CryptoAssetId);
        var cryptoAsset = await _cryptoAssetRepository.GetByIdAsync(request.CryptoAssetId,
                                                                    x => x.Include(q => q.Transactions));
        if (cryptoAsset is null)
        {
            _logger.LogError("GetCryptoAssetByIdCommandQuery. CryptoAssetId: {0} not found", request.CryptoAssetId);
            return new Response("Crypto asset not found", false);
        }

        GetQuoteResponse cmpResponse;
        try
        {
            cmpResponse = await _coinMarketCapService.GetQuotesByIds(new[] { cryptoAsset.CoinMarketCapId.ToString() });
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "GetCryptoAssetByIdCommandQuery. Error getting quotes for CryptoAssetId: {0}", request.CryptoAssetId);
            return new Response($"Error getting quotes for this crypto asset. Error: {ex.Message}", false);
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
        var cryptoInfo = new ViewCryptoAssetDto(cryptoAsset.Id,
                                                new ViewCryptoInformation(cryptoAsset.Symbol.ToLower(),
                                                                          cryptoAsset.CoinMarketCapId),
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
                                                                                                     a.AddressValue)).ToList());

        _logger.LogInformation("GetCryptoAssetByIdCommandQuery. Retrieved CryptoAssetId: {0}", request.CryptoAssetId);
        return new Response("", true, cryptoInfo);
    }
}
