using api.Cryptos.Repositories;
using api.Shared;
using MediatR;

namespace api.Cryptos.Queries;
public record GetCryptosCommandQuery() : IRequest<Response>;
public class GetCryptosCommandQueryHandler : IRequestHandler<GetCryptosCommandQuery, Response>
{
    private readonly ICryptoRepository _cryptoRepository;
    private readonly ILogger<GetCryptosCommandQueryHandler> _logger;

    public GetCryptosCommandQueryHandler(ICryptoRepository cryptoRepository,
                                         ILogger<GetCryptosCommandQueryHandler> logger)
    {
        _cryptoRepository = cryptoRepository;
        _logger = logger;
    }

    public async Task<Response> Handle(GetCryptosCommandQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCryptosCommandQuery. Retrieving all cryptos.");
        var cryptos = _cryptoRepository.GetAll();
        await Task.WhenAll();
        return new Response("", true, cryptos);
    }
}
