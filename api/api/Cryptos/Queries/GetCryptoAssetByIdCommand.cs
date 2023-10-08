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

    public GetCryptoAssetByIdCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(GetCryptoAssetByIdCommand request, CancellationToken cancellationToken)
    {
        var cryptoAsset = await _context.CryptoAssets
                                        .Include(x => x.Transactions)
                                        .Include(x => x.Addresses)
                                        .Where(x => x.Id == request.CryptoAssetId && !x.Deleted)
                                        .Select(x => new ViewCryptoAssetDto(x.Id,
                                                                            x.CurrencyName,
                                                                            x.CryptoCurrency,
                                                                            x.Symbol,
                                                                            x.CreatedAt,
                                                                            x.Transactions.Select(x => new ViewCryptoTransactionDto(x.Amount,
                                                                                                                                    x.Price,
                                                                                                                                    x.PurchaseDate,
                                                                                                                                    x.ExchangeName,
                                                                                                                                    x.TransactionType)).ToList(),
                                                                            x.Balance,
                                                                            x.Addresses.Select(a => new ViewAddressDto(a.Id,
                                                                                                                       a.AddressName,
                                                                                                                       a.AddressValue)).ToList(),
                                                                            x.GetAveragePrice()))
                                        .FirstOrDefaultAsync(cancellationToken);
        if (cryptoAsset is null)
            return new Response("Crypto asset not found", false);

        return new Response("ok", true, cryptoAsset);
    }
}
