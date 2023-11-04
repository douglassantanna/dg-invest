using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetCryptoAssetByIdCommand(int CryptoAssetId) : IRequest<Response>;
public class GetCryptoAssetByIdCommandHandler : IRequestHandler<GetCryptoAssetByIdCommand, Response>
{
    private readonly DataContext _context;
    private readonly ICoinMarketCapService _coinMarketCapService;

    public GetCryptoAssetByIdCommandHandler(
        DataContext context,
        ICoinMarketCapService coinMarketCapService)
    {
        _context = context;
        _coinMarketCapService = coinMarketCapService;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdCommand request, CancellationToken cancellationToken)
    {
        var cryptoAsset = await _context.CryptoAssets
                                        .Include(x => x.Transactions)
                                        .Include(x => x.Addresses)
                                        .Where(x => x.Id == request.CryptoAssetId && !x.Deleted)
                                        .FirstOrDefaultAsync(cancellationToken);
        if (cryptoAsset is null)
        {
            return new Response("Crypto asset not found", false);
        }

        var cmpResponse = await _coinMarketCapService.GetQuotesByIds(new[] { cryptoAsset.CoinMarketCapId.ToString() });

        var currentPrice = GetPercentageChange24hById(cryptoAsset.CoinMarketCapId, cmpResponse);

        var cryptoInfo = new ViewCryptoAssetDto(cryptoAsset.Id,
                                                new ViewCryptoInformation(cryptoAsset.Symbol,
                                                                          currentPrice,
                                                                          cryptoAsset.GetAveragePrice(),
                                                                          cryptoAsset.GetPercentDifference(currentPrice),
                                                                          cryptoAsset.Balance,
                                                                          cryptoAsset.TotalInvested,
                                                                          cryptoAsset.CurrentWorth(currentPrice, dollarValue: 5),
                                                                          1234,
                                                                          cryptoAsset.CoinMarketCapId),
                                                cryptoAsset.Transactions.Select(t => new ViewCryptoTransactionDto(t.Amount,
                                                                                                                  t.Price,
                                                                                                                  t.PurchaseDate,
                                                                                                                  t.ExchangeName,
                                                                                                                  t.TransactionType)).ToList(),
                                                cryptoAsset.Addresses.Select(a => new ViewAddressDto(a.Id,
                                                                                                     a.AddressName,
                                                                                                     a.AddressValue)).ToList());
        if (cryptoAsset is null)
            return new Response("Crypto asset not found", false);

        return new Response("", true, cryptoAsset);
    }
    private static decimal GetPercentageChange24hById(int coinMarketCapId, GetQuoteResponse cmpResponse)
    {
        var coin = cmpResponse.Data.FirstOrDefault(coin => coin.Key.ToString() == coinMarketCapId.ToString());
        if (coin.Value != null)
        {
            return coin.Value.Quote.USD.Percent_change_24h;
        }
        return 0;
    }
}
