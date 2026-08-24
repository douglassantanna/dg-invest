using api.Data;
using api.Exchanges.Models;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public record ToggleBybitAccountCommand(int UserId, int AccountId) : IRequest<Response>;

public class ToggleBybitAccountCommandHandler : IRequestHandler<ToggleBybitAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<ToggleBybitAccountCommandHandler> _logger;

    public ToggleBybitAccountCommandHandler(DataContext context, ILogger<ToggleBybitAccountCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(ToggleBybitAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsDeleted
                     && a.AccountType == api.Cryptos.Models.EAccountType.Exchange && a.Exchange == "Bybit")
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            _logger.LogError("ToggleBybitAccount: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            return new Response("Account not found", false, 404);
        }

        var syncStatus = await _context.SyncStatuses
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.AccountId == request.AccountId && s.ExchangeName == "Bybit", cancellationToken);

        if (syncStatus == null)
        {
            return new Response("No Bybit configuration found for this account", false, 404);
        }

        if (!syncStatus.IsEnabled)
        {
            if (syncStatus.ActiveCredentialSetId == null)
                return new Response("Cannot enable account without active Bybit credentials", false, 400);

            var integration = await _context.ExchangeIntegrations
                .Where(x => x.UserId == request.UserId && x.Exchange == "Bybit")
                .Select(x => new { x.Enabled, x.ActiveCredentialSetId })
                .SingleOrDefaultAsync(cancellationToken);
            if (integration is not null && (!integration.Enabled || integration.ActiveCredentialSetId == null))
                return new Response("Cannot enable account while Bybit integration is disconnected", false, 400);
        }

        syncStatus.ToggleEnabled();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("ToggleBybitAccount: account {AccountId} IsEnabled = {IsEnabled}", request.AccountId, syncStatus.IsEnabled);
        return new Response("ok", true, new { IsEnabled = syncStatus.IsEnabled });
    }
}
