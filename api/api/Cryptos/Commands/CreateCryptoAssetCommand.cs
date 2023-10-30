using System.Text;
using api.Models.Cryptos;
using FluentValidation;
using FluentValidation.Results;
using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    private readonly DataContext _context;
    private readonly ILogger<CreateCryptoAssetCommandHandler> _logger;

    public CreateCryptoAssetCommandHandler(
        DataContext context,
        ILogger<CreateCryptoAssetCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(CreateCryptoAssetCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        if (await CryptoAssetExists(request))
            return new Response("Asset already exists", false);

        var stringBuilder = new StringBuilder();
        stringBuilder.Append(request.Crypto);
        stringBuilder.Append(request.Currency);
        var cryptoAssetName = stringBuilder.ToString();

        var cryptoAsset = new CryptoAsset(request.Crypto,
                                          request.Currency,
                                          cryptoAssetName,
                                          request.CoinMarketCapId);

        _context.CryptoAssets.Add(cryptoAsset);
        await _context.SaveChangesAsync();

        return new Response("ok", true, cryptoAsset.Id);
    }

    private async Task<bool> CryptoAssetExists(CreateCryptoAssetCommand request)
    {
        return await _context.CryptoAssets.AnyAsync(x => x.CoinMarketCapId == request.CoinMarketCapId);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateCryptoAssetCommand request)
    {
        var validation = new CreateCryptoAssetCommandValidator();
        return await validation.ValidateAsync(request);
    }
}