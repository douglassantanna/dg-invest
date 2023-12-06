using api.Cryptos.Models;
using api.Data.Repositories;
using api.Shared;
using MediatR;

namespace api.Cryptos.Queries;
public record GetCryptosCommandQuery() : IRequest<Response>;
public class GetCryptosCommandQueryHandler : IRequestHandler<GetCryptosCommandQuery, Response>
{
    private readonly IBaseRepository<Crypto> _cryptoRepository;

    public GetCryptosCommandQueryHandler(IBaseRepository<Crypto> cryptoRepository)
    {
        _cryptoRepository = cryptoRepository;
    }

    public async Task<Response> Handle(GetCryptosCommandQuery request, CancellationToken cancellationToken)
    {
        var cryptos = _cryptoRepository.GetAll();
        await Task.WhenAll();
        return new Response("", true, cryptos);
    }
}
