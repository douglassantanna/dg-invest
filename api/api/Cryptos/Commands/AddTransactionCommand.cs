using api.Data;
using api.Models.Cryptos;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    }
}
public class AddTransactionCommandHandler : IRequestHandler<AddTransactionCommand, Response>
{
    private readonly DataContext _context;

    public AddTransactionCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        var cryptoAsset = await _context.CryptoAssets.Where(x => x.Id == request.CryptoAssetId).FirstOrDefaultAsync(cancellationToken);
        if (cryptoAsset == null)
            return new Response("Crypto asset not found", false);

        var transaction = new CryptoTransaction(request.Amount,
                                                request.Price,
                                                request.PurchaseDate,
                                                request.ExchangeName,
                                                request.TransactionType);

        cryptoAsset.AddTransaction(transaction);

        await _context.CryptoTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new Response("ok", true, transaction.Id);
    }

    private async Task<ValidationResult> ValidateRequestAsync(AddTransactionCommand request)
    {
        var validation = new AddTransactionCommandValidator();
        return await validation.ValidateAsync(request);
    }
}