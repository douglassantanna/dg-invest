using System.Threading;
using System.Threading.Tasks;
using api.Models.Cryptos;
using FluentValidation;
using FluentValidation.Results;
using function_api.Data;
using function_api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace function_api.Cryptos.Commands;
public record CreateCryptoAssetCommand(string Crypto, string Currency) : IRequest<Response>;

public class CreateCryptoAssetCommandValidator : AbstractValidator<CreateCryptoAssetCommand>
{
    public CreateCryptoAssetCommandValidator()
    {
        RuleFor(x => x.Crypto).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
    }
}
public class CreateCryptoAssetCommandHandler : IRequestHandler<CreateCryptoAssetCommand, Response>
{
    private readonly DataContext _context;

    public CreateCryptoAssetCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(CreateCryptoAssetCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors);

        if (await CryptoAssetExists(request))
            return new Response("Asset already exists", false);

        var cryptoAsset = new CryptoAsset(request.Crypto,
                                          request.Currency,
                                          request.Crypto + request.Currency);

        await _context.CryptoAssets.AddAsync(cryptoAsset);
        await _context.SaveChangesAsync();

        return new Response("ok", true, cryptoAsset.Id);
    }

    private async Task<bool> CryptoAssetExists(CreateCryptoAssetCommand request)
    {
        return await _context.CryptoAssets.AnyAsync(x => x.CryptoCurrency == request.Crypto && x.CurrencyName == request.Currency);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateCryptoAssetCommand request)
    {
        var validation = new CreateCryptoAssetCommandValidator();
        return await validation.ValidateAsync(request);
    }
}