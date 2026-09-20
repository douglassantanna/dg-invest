using api.Data;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public sealed record SyncBybitAccountCommand(int UserId, int AccountId) : IRequest<Response>;

public sealed class SyncBybitAccountCommandHandler : IRequestHandler<SyncBybitAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly IBybitAccountSyncService _accountSyncService;

    public SyncBybitAccountCommandHandler(DataContext context, IBybitAccountSyncService accountSyncService)
    {
        _context = context;
        _accountSyncService = accountSyncService;
    }

    public async Task<Response> Handle(SyncBybitAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(candidate => candidate.CryptoAssets)
                .ThenInclude(asset => asset.Transactions)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AccountId
                && candidate.UserId == request.UserId
                && !candidate.IsDeleted
                && candidate.AccountType == api.Cryptos.Models.EAccountType.Exchange
                && candidate.Exchange == "Bybit", cancellationToken);

        if (account is null)
            return new Response("Bybit exchange account not found", false, 404);

        var result = await _accountSyncService.SyncAsync(account, cancellationToken);
        return result.IsSuccess
            ? new Response(result.Message, true)
            : new Response(result.Message, false, result.StatusCode);
    }
}
