using api.Data;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record RenameBybitAccountCommand(int UserId, int AccountId, string Name) : IRequest<Response>;

public class RenameBybitAccountCommandValidator : AbstractValidator<RenameBybitAccountCommand>
{
    public RenameBybitAccountCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public class RenameBybitAccountCommandHandler : IRequestHandler<RenameBybitAccountCommand, Response>
{
    private readonly DataContext _context;

    public RenameBybitAccountCommandHandler(DataContext context) => _context = context;

    public async Task<Response> Handle(RenameBybitAccountCommand request, CancellationToken cancellationToken)
    {
        var validation = await new RenameBybitAccountCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return new Response("Validation failed", false, validation.Errors.Select(error => error.ErrorMessage).ToList());

        var account = await _context.Accounts
            .FirstOrDefaultAsync(account => account.Id == request.AccountId
                && account.UserId == request.UserId
                && !account.IsDeleted,
                cancellationToken);

        if (account is null)
            return new Response("Account not found", false, 404);

        if (account.AccountType != api.Cryptos.Models.EAccountType.Exchange || account.Exchange != "Bybit")
            return new Response("Account is not an active Bybit exchange account", false, 400);

        var name = request.Name.Trim();
        var duplicate = await _context.Accounts.AnyAsync(existing => existing.UserId == request.UserId
            && existing.Id != request.AccountId
            && existing.Name == name
            && !existing.IsDeleted,
            cancellationToken);
        if (duplicate)
            return new Response($"An account with the name '{name}' already exists", false, 400);

        account.SetName(name);
        await _context.SaveChangesAsync(cancellationToken);
        return new Response("Account nickname saved", true);
    }
}
