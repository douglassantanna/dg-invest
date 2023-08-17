using System;
using System.Threading;
using System.Threading.Tasks;
using api.Models.Cryptos;
using FluentValidation;
using FluentValidation.Results;
using function_api.Data;
using function_api.Shared;
using MediatR;

namespace function_api.Cryptos.Commands;
public record CreateTransactionCommand(decimal Amount,
                                       decimal Price,
                                       DateTimeOffset PurchaseDate,
                                       string ExchangeName,
                                       ETransactionType TransactionType) : IRequest<Response>;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
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
            return new Response("Validation failed", false, validationResult.Errors);

        var transaction = new CryptoTransaction(request.Amount,
                                                request.Price,
                                                request.PurchaseDate,
                                                request.ExchangeName,
                                                request.TransactionType);

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