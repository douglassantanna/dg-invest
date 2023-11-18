using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Data.Repositories;
using api.Models.Cryptos;
using api.Shared;
using MediatR;

namespace api.Cryptos.Queries;
public record GetCryptoAssetByIdCommandQuery(int CryptoAssetId) : IRequest<Response>;
public class GetCryptoAssetByIdCommandQueryHandler : IRequestHandler<GetCryptoAssetByIdCommandQuery, Response>
{
    private readonly IBaseRepository<CryptoAsset> _cryptoAssetRepository;
    private readonly ICoinMarketCapService _coinMarketCapService;

    public GetCryptoAssetByIdCommandQueryHandler(
        ICoinMarketCapService coinMarketCapService,
        IBaseRepository<CryptoAsset> cryptoAssetRepository)
    {
        _coinMarketCapService = coinMarketCapService;
        _cryptoAssetRepository = cryptoAssetRepository;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdCommandQuery request, CancellationToken cancellationToken)
    {
        var cryptoAsset = await _cryptoAssetRepository.GetByIdAsync(request.CryptoAssetId, cancellationToken);
        if (cryptoAsset is null)
        {
            return new Response("Crypto asset not found", false);
        }

        var cmpResponse = await _coinMarketCapService.GetQuotesByIds(new[] { cryptoAsset.CoinMarketCapId.ToString() });

        var currentPrice = GetCryptoCurrentPriceById(cryptoAsset.CoinMarketCapId, cmpResponse);

        var cryptoInfo = new ViewCryptoAssetDto(cryptoAsset.Id,
                                                new ViewCryptoInformation(cryptoAsset.Symbol,
                                                                          currentPrice,
                                                                          cryptoAsset.AveragePrice,
                                                                          cryptoAsset.GetPercentDifference(currentPrice),
                                                                          cryptoAsset.Balance,
                                                                          cryptoAsset.TotalInvested,
                                                                          cryptoAsset.CurrentWorth(currentPrice),
                                                                          cryptoAsset.GetInvestmentGainLoss(),
                                                                          cryptoAsset.CoinMarketCapId),
                                                cryptoAsset.Transactions.Select(t => new ViewCryptoTransactionDto(t.Amount,
                                                                                                                  t.Price,
                                                                                                                  t.PurchaseDate,
                                                                                                                  t.ExchangeName,
                                                                                                                  t.TransactionType)).ToList(),
                                                cryptoAsset.Addresses.Select(a => new ViewAddressDto(a.Id,
                                                                                                     a.AddressName,
                                                                                                     a.AddressValue)).ToList());

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
