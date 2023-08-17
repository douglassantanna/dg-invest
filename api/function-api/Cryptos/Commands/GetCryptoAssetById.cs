using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using function_api.Cryptos.Dtos;
using function_api.Data;
using function_api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace function_api.Cryptos.Commands;
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
                                        .Where(x => x.Id == request.CryptoAssetId)
                                        .Select(x => new ViewMinimalCryptoAssetDto(x.Id,
                                                                                   x.CurrencyName,
                                                                                   x.CryptoCurrency,
                                                                                   x.Symbol))
                                        .FirstOrDefaultAsync(cancellationToken);
        if (cryptoAsset is null)
            return new Response("Crypto asset not found", false);

        return new Response("ok", true, cryptoAsset);
    }
}
