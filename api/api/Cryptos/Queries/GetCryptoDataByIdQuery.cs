using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Repositories;
using api.Shared;
using Flurl.Http;
using MediatR;

namespace api.Cryptos.Queries;

public record GetCryptoDataByIdQuery(int CryptoAssetId) : IRequest<Response>;
public class GetCryptoDataByIdHandler : IRequestHandler<GetCryptoDataByIdQuery, Response>
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ILogger<GetCryptoDataByIdHandler> _logger;

    public GetCryptoDataByIdHandler(
    ICoinMarketCapService coinMarketCapService,
    ICryptoAssetRepository cryptoAssetRepository,
    ILogger<GetCryptoDataByIdHandler> logger)
    {
        _coinMarketCapService = coinMarketCapService;
        _cryptoAssetRepository = cryptoAssetRepository;
        _logger = logger;
    }
    public async Task<Response> Handle(GetCryptoDataByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCryptoDataByIdQuery. Retrieving GetCryptoDataById: {0}", request.CryptoAssetId);
        var cryptoAsset = await _cryptoAssetRepository.GetByIdAsync(request.CryptoAssetId);
        if (cryptoAsset is null)
        {
            _logger.LogError("GetCryptoDataByIdQuery. CryptoAsset not found: {0}", request.CryptoAssetId);
            return new Response("Crypto asset not found", false);
        }

        GetQuoteResponse cmpResponse;
        try
        {
            cmpResponse = await _coinMarketCapService.GetQuotesByIds(new[] { cryptoAsset.CoinMarketCapId.ToString() });
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError("GetCryptoDataByIdQuery. Error retrieving CoinMarketCap data for CryptoAsset: {0}", request.CryptoAssetId);
            return new Response($"Error retrieving CoinMarketCap data for this crypto asset. Error: {ex.Message}", false);
        }

        var currentPrice = GetCryptoCurrentPriceById(cryptoAsset.CoinMarketCapId, cmpResponse);

        List<CryptoAssetData> cards = new()
        {
            new CryptoAssetData("Current price", currentPrice),
            new CryptoAssetData("Average price", cryptoAsset.AveragePrice, cryptoAsset.GetPercentDifference(currentPrice)),
            new CryptoAssetData("Balance", cryptoAsset.Balance),
            new CryptoAssetData("Invested amount", cryptoAsset.TotalInvested),
            new CryptoAssetData("Current worth", cryptoAsset.CurrentWorth(currentPrice)),
            new CryptoAssetData("Gain/Loss", cryptoAsset.GetInvestmentGainLoss(currentPrice)),
        };

        var cryptoInfo = new ViewCryptoDataDto(cryptoAsset.Id, cards);

        _logger.LogInformation("GetCryptoDataByIdQuery. Retrieved GetCryptoDataById: {0}", request.CryptoAssetId);
        return new Response("", true, cryptoInfo);
    }

    private static decimal GetCryptoCurrentPriceById(int coinMarketCapId, GetQuoteResponse cmpResponse)
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
}