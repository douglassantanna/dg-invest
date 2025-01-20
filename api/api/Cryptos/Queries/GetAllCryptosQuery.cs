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
    private readonly ILogger<GetAllCryptosQueryHandler> _logger;

    public GetAllCryptosQueryHandler(ILogger<GetAllCryptosQueryHandler> logger,
                                     DataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Response> Handle(GetAllCryptosQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllCryptosQuery. Retrieving all cryptos.");
        var cryptos = await _context.Cryptos.AsNoTracking()
                                            .Select(c => new ViewCryptoDto(c.Id, c.Symbol.ToLower(), c.Name, c.Symbol.ToLower(), c.CoinMarketCapId))
                                            .ToListAsync(cancellationToken);
        return new Response("", true, cryptos);
    }
}
