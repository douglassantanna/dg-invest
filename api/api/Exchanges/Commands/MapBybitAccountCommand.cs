using api.Data;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record MapBybitAccountCommand(int UserId, int AccountId, string BybitUid) : IRequest<Response>;

public class MapBybitAccountCommandValidator : AbstractValidator<MapBybitAccountCommand>
{
    public MapBybitAccountCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.BybitUid).NotEmpty().MaximumLength(50);
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
            .AnyAsync(a => a.BybitUid == request.BybitUid && a.Id != request.AccountId, cancellationToken);
        if (alreadyMapped)
            return new Response($"Bybit UID '{request.BybitUid}' is already mapped to another account", false, 400);

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
        {
            _logger.LogError("MapBybitAccount: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        account.SetBybitUid(request.BybitUid);
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MapBybitAccount: account '{Tag}' (id {AccountId}) mapped to Bybit UID {Uid}",
            account.SubaccountTag, account.Id, request.BybitUid);

        return new Response($"Account '{account.SubaccountTag}' linked to Bybit UID {request.BybitUid}", true);
    }
}
