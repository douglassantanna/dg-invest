using System.Text;
using api.Models.Cryptos;
using FluentValidation;
using FluentValidation.Results;
using api.Shared;
using MediatR;
using api.Data.Repositories;

namespace api.Cryptos.Commands;
public record CreateCryptoAssetCommand(string Crypto, string Currency, int CoinMarketCapId) : IRequest<Response>;

public class CreateCryptoAssetCommandValidator : AbstractValidator<CreateCryptoAssetCommand>
{
    public CreateCryptoAssetCommandValidator()
    {
        RuleFor(x => x.Crypto).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.CoinMarketCapId).NotNull();
    }
}
public class CreateCryptoAssetCommandHandler : IRequestHandler<CreateCryptoAssetCommand, Response>
{
    private readonly IBaseRepository<CryptoAsset> _cryptoAssetRepository;

    private readonly ILogger<CreateCryptoAssetCommandHandler> _logger;

    public CreateCryptoAssetCommandHandler(
        ILogger<CreateCryptoAssetCommandHandler> logger,
        IBaseRepository<CryptoAsset> cryptoAssetRepository)
    {
        _logger = logger;
        _cryptoAssetRepository = cryptoAssetRepository;
    }

    public async Task<Response> Handle(CreateCryptoAssetCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateCryptoAssetCommandHandler");
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogInformation("CreateCryptoAssetCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        if (await CryptoAssetExists(request, cancellationToken))
        {
            _logger.LogInformation("CreateCryptoAssetCommandHandler. Asset already exists: {0}", request.CoinMarketCapId);
            return new Response("Asset already exists", false);
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append(request.Crypto);
        stringBuilder.Append(request.Currency);
        var cryptoAssetName = stringBuilder.ToString();

        var cryptoAsset = new CryptoAsset(request.Crypto,
                                          request.Currency,
                                          cryptoAssetName,
                                          request.CoinMarketCapId);

        _cryptoAssetRepository.Add(cryptoAsset);
        await _cryptoAssetRepository.UpdateAsync(cryptoAsset);

        return new Response("ok", true, cryptoAsset.Id);
    }

    private async Task<bool> CryptoAssetExists(CreateCryptoAssetCommand request,
                                               CancellationToken cancellationToken)
    => await _cryptoAssetRepository.GetByCoinMarketCapIdAsync(request.CoinMarketCapId, cancellationToken);

    private async Task<ValidationResult> ValidateRequestAsync(CreateCryptoAssetCommand request)
    {
        var validation = new CreateCryptoAssetCommandValidator();
        return await validation.ValidateAsync(request);
    }
}