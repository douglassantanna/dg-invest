using function_api.Shared;
using MediatR;

namespace function_api.Cryptos.Commands;
public record GetCryptoAssetById() : IRequest<Response>;