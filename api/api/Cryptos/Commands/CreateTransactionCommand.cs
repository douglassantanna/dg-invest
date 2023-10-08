using api.Data;
using api.Models.Cryptos;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record CreateTransactionCommand(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType,
                                       int CryptoAssetId) : IRequest<Response>;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CryptoAssetId).GreaterThan(1);
        RuleFor(x => x.ExchangeName).NotEmpty();
        RuleFor(x => x.TransactionType).IsInEnum();
    }
}
public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Response>
{
    private readonly DataContext _context;

    public CreateTransactionCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
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

    private async Task<ValidationResult> ValidateRequestAsync(CreateTransactionCommand request)
    {
        var validation = new CreateTransactionCommandValidator();
        return await validation.ValidateAsync(request);
    }
}