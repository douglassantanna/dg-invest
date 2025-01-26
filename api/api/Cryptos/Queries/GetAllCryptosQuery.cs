using api.Cryptos.Dtos;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace api.Cryptos.Queries;
public record GetAllCryptosQuery() : IRequest<Response>;
public class GetAllCryptosQueryHandler : IRequestHandler<GetAllCryptosQuery, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<GetAllCryptosQueryHandler> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AllCryptos";

    public GetAllCryptosQueryHandler(ILogger<GetAllCryptosQueryHandler> logger,
                                     DataContext context,
                                     IMemoryCache cache)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
    }

    public async Task<Response> Handle(GetAllCryptosQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllCryptosQuery. Retrieving all cryptos.");
        if (!_cache.TryGetValue(CacheKey, out List<ViewCryptoDto>? cryptos))
        {
            _logger.LogInformation("Cache miss. Querying database.");

            cryptos = await _context.Cryptos.AsNoTracking()
                                             .Select(c => new ViewCryptoDto(c.Id, c.Symbol.ToLower(), c.Name, c.Symbol.ToLower(), c.CoinMarketCapId))
                                             .ToListAsync(cancellationToken);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            _cache.Set(CacheKey, cryptos, cacheOptions);
        }
        else
        {
            _logger.LogInformation("Cache hit. Returning cached data.");
        }

        return new Response("", true, cryptos);
    }
}
