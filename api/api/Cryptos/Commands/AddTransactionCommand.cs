using api.Cryptos.Exceptions;
using api.Data.Repositories;
using api.Models.Cryptos;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace api.Cryptos.Commands;
public record AddTransactionCommand(decimal Amount,
                                    decimal Price,
                                    DateTimeOffset PurchaseDate,
                                    string ExchangeName,
                                    ETransactionType TransactionType,
                                    int CryptoAssetId) : IRequest<Response>;

public class AddTransactionCommandValidator : AbstractValidator<AddTransactionCommand>
{
    public AddTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CryptoAssetId).GreaterThan(0);
        RuleFor(x => x.ExchangeName).NotEmpty();
        RuleFor(x => x.TransactionType).IsInEnum();
        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Purchase date can't be empty")
            .Must(BeInPastOrPresent)
            .WithMessage("Purchase date must be in the present or in the past");
    }
    private bool BeInPastOrPresent(DateTimeOffset purchaseDate)
    {
        return purchaseDate <= DateTime.Now;
    }
}
public class AddTransactionCommandHandler : IRequestHandler<AddTransactionCommand, Response>
{
    private readonly IBaseRepository<CryptoAsset> _cryptoAssetRepository;

    public AddTransactionCommandHandler(IBaseRepository<CryptoAsset> cryptoAssetRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository;
    }

    public async Task<Response> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return new Response("Validation failed", false, errors);
        }

        var cryptoAsset = _cryptoAssetRepository.GetById(request.CryptoAssetId);
        if (cryptoAsset == null)
            return new Response("Crypto asset not found", false);

        var transaction = new CryptoTransaction(request.Amount,
                                                request.Price,
                                                request.PurchaseDate,
                                                request.ExchangeName,
                                                request.TransactionType);

        try
        {
            cryptoAsset.AddTransaction(transaction);
        }
        catch (CryptoAssetException ex)
        {
            return new Response(ex.Message, false);
        }

        _cryptoAssetRepository.Add(cryptoAsset);
        await _cryptoAssetRepository.UpdateAsync(cryptoAsset);

        return new Response("ok", true, cryptoAsset);
    }

    private async Task<ValidationResult> ValidateRequestAsync(AddTransactionCommand request)
    {
        var validation = new AddTransactionCommandValidator();
        return await validation.ValidateAsync(request);
    }
}