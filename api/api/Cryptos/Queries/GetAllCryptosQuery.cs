using api.Cache;
using api.Cryptos.Dtos;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetAllCryptosQuery() : IRequest<Response>;
public class GetAllCryptosQueryHandler : IRequestHandler<GetAllCryptosQuery, Response>
{
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;
    private const string CacheKey = "AllCryptos";

    public GetAllCryptosQueryHandler(DataContext context,
                                     ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(GetAllCryptosQuery request, CancellationToken cancellationToken)
    {
        var absoluteExpiration = TimeSpan.FromMinutes(10);
        var cryptos = await _cacheService.GetOrCreateAsync(CacheKey, async (ct) =>
        {
            return await _context.Cryptos
                .AsNoTracking()
                .Select(c => new ViewCryptoDto(c.Id, c.Symbol.ToLower(), c.Name, c.Symbol.ToLower(), c.CoinMarketCapId))
                .ToListAsync(ct);
        },
        absoluteExpiration: absoluteExpiration,
        cancellationToken: cancellationToken);

        return new Response("", true, cryptos);
    }
}
