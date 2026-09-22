using api.Data;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record MapBybitAccountCommand(int UserId, int AccountId, string ExternalId) : IRequest<Response>;

public class MapBybitAccountCommandValidator : AbstractValidator<MapBybitAccountCommand>
{
    public MapBybitAccountCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.ExternalId).NotEmpty().MaximumLength(50);
    }
}

public class MapBybitAccountCommandHandler : IRequestHandler<MapBybitAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<MapBybitAccountCommandHandler> _logger;

    public MapBybitAccountCommandHandler(DataContext context, ILogger<MapBybitAccountCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(MapBybitAccountCommand request, CancellationToken cancellationToken)
    {
        var validator = new MapBybitAccountCommandValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return new Response("Validation failed", false, errors);
        }

        // Ensure the UID is not already mapped to a different account.
        var alreadyMapped = await _context.Accounts
            .AnyAsync(a => a.UserId == request.UserId
                           && a.Exchange == "Bybit"
                           && a.ExternalId == request.ExternalId
                           && a.Id != request.AccountId
                           && !a.IsDeleted,
                cancellationToken);
        if (alreadyMapped)
            return new Response($"Bybit UID '{request.ExternalId}' is already mapped to another account", false, 400);

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsDeleted, cancellationToken);

        if (account == null)
        {
            _logger.LogError("MapBybitAccount: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        if (account.AccountType != api.Cryptos.Models.EAccountType.Exchange || account.Exchange != "Bybit")
            return new Response("Account is not an active Bybit exchange account", false, 400);

        account.SetExternalId(request.ExternalId);
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MapBybitAccount: account '{Name}' (id {AccountId}) mapped to Bybit UID {Uid}",
            account.Name, account.Id, request.ExternalId);

        return new Response($"Account '{account.Name}' linked to Bybit UID {request.ExternalId}", true);
    }
}
