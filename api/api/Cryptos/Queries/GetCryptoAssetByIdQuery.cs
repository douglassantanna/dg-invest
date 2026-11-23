using api.Models.Cryptos;
using Microsoft.Extensions.Options;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Repositories;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetCryptoAssetByIdQuery(int CryptoAssetId) : IRequest<Response>;
// Not cached: the asset is read from the database on every request; only the price is shared, through IPriceService.
public class GetCryptoAssetByIdQueryHandler : IRequestHandler<GetCryptoAssetByIdQuery, Response>
{
    private readonly TransferCostPolicy _transferCostPolicy;
    private readonly ICryptoAssetRepository _cryptoAssetRepository;
    private readonly IPriceService _priceService;
    private readonly ILogger<GetCryptoAssetByIdQueryHandler> _logger;

    public GetCryptoAssetByIdQueryHandler(
        IPriceService priceService,
        ICryptoAssetRepository cryptoAssetRepository,
        ILogger<GetCryptoAssetByIdQueryHandler> logger,
        IOptions<CostBasisSettings>? costBasisSettings = null)
    {
        _priceService = priceService;
        _cryptoAssetRepository = cryptoAssetRepository;
        _logger = logger;
        _transferCostPolicy = costBasisSettings?.Value.TransferCostPolicy ?? TransferCostPolicy.Legacy;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var cryptoAsset = await _cryptoAssetRepository.GetByIdAsync(request.CryptoAssetId, x => x.Include(q => q.Transactions));
        if (cryptoAsset is null)
        {
            _logger.LogWarning("GetCryptoAssetByIdQuery. CryptoAssetId: {0} not found", request.CryptoAssetId);
            return new Response("Error getting crypto asset", false);
        }

        var prices = await _priceService.GetPricesAsync([cryptoAsset.CoinMarketCapId], cancellationToken);
        // Without any price (CoinMarketCap down and none known yet), every figure below would be wrong.
        if (prices.FetchFailed && !prices.Prices.ContainsKey(cryptoAsset.CoinMarketCapId))
        {
            _logger.LogError("GetCryptoAssetByIdQuery. No price available for CryptoAssetId: {0}", request.CryptoAssetId);
            return new Response("Error getting crypto asset", false);
        }

        var currentPrice = prices.PriceOf(cryptoAsset.CoinMarketCapId);
        var costBasis = cryptoAsset.GetCostBasis(currentPrice, _transferCostPolicy);
        List<CryptoAssetData> cards = new()
        {
            new CryptoAssetData("Current price", currentPrice),
            new CryptoAssetData("Average price", costBasis.AveragePrice),
            new CryptoAssetData("Balance", cryptoAsset.Balance),
            new CryptoAssetData("Invested amount", costBasis.Invested),
            new CryptoAssetData("Current worth", cryptoAsset.CurrentWorth(currentPrice)),
            new CryptoAssetData("Gain/Loss", costBasis.GainLoss, costBasis.GainLossPercentage),
        };
        // Quantity Bybit holds that no recorded transaction explains; valued at the current price above.
        if (costBasis.UnexplainedBalance > 0)
            cards.Add(new CryptoAssetData(CryptoAssetData.WithoutHistoryTitle, costBasis.UnexplainedBalance));

        var cryptoInfo = new ViewCryptoAssetDto(
            cryptoAsset.Id,
            new ViewCryptoInformation(cryptoAsset.Symbol.ToLower(), cryptoAsset.CoinMarketCapId),
            cards,
            cryptoAsset.Transactions.Select(t => new ViewCryptoTransactionDto(t.Id,
                                                                            t.Amount,
                                                                            t.Price,
                                                                            t.PurchaseDate,
                                                                            t.ExchangeName,
                                                                            t.TransactionType,
                                                                            t.GetPercentDifference(currentPrice),
                                                                            t.Fee)).ToList());
        return new Response("", true, cryptoInfo);
    }
}
