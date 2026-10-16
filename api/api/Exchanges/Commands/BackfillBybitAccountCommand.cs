using api.Data;
using api.Exchanges.Services;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Commands;

public sealed record BackfillBybitAccountCommand(int UserId, int AccountId, DateOnly FromDate) : IRequest<Response>;

public sealed class BackfillBybitAccountCommandHandler : IRequestHandler<BackfillBybitAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly IBybitAccountSyncService _accountSyncService;

    public BackfillBybitAccountCommandHandler(DataContext context, IBybitAccountSyncService accountSyncService)
    {
        _context = context;
        _accountSyncService = accountSyncService;
    }

    public async Task<Response> Handle(BackfillBybitAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(candidate => candidate.CryptoAssets)
                .ThenInclude(asset => asset.Transactions)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AccountId
                && candidate.UserId == request.UserId
                && !candidate.IsDeleted
                && candidate.Enabled
                && candidate.AccountType == api.Cryptos.Models.EAccountType.Exchange
                && candidate.Exchange == "Bybit", cancellationToken);

        if (account is null)
            return new Response("Bybit exchange account not found", false, 404);

        var fromUtc = request.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var result = await _accountSyncService.BackfillAsync(account, fromUtc, cancellationToken);
        return result.IsSuccess
            ? new Response(result.Message, true)
            : new Response(result.Message, false, result.StatusCode);
    }
}
