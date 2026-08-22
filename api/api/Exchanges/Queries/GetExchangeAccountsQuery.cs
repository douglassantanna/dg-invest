using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetExchangeAccountsQuery(int UserId) : IRequest<Response>;

public record ExchangeAccountDto(
    int AccountId,
    string AccountName,
    string ExchangeName,
    string Status,
    DateTime? LastSyncAt,
    int ErrorCount,
    string? LastErrorMessage)
{
    public string AccountTag => AccountName;
}

public class GetExchangeAccountsQueryHandler : IRequestHandler<GetExchangeAccountsQuery, Response>
{
    private readonly DataContext _context;

    public GetExchangeAccountsQueryHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(GetExchangeAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId && !a.IsDeleted && a.AccountType == api.Cryptos.Models.EAccountType.Exchange)
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(cancellationToken);

        var syncStatuses = await _context.SyncStatuses
            .Where(s => s.UserId == request.UserId)
            .Select(s => new { s.AccountId, s.ExchangeName, s.Status, s.LastSyncAt, s.ErrorCount, s.LastErrorMessage })
            .ToListAsync(cancellationToken);

        var statusLookup = syncStatuses
            .GroupBy(s => s.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ExchangeAccountDto>();

        foreach (var account in accounts)
        {
            if (statusLookup.TryGetValue(account.Id, out var statuses))
            {
                foreach (var s in statuses)
                {
                    result.Add(new ExchangeAccountDto(
                        account.Id,
                        account.Name,
                        s.ExchangeName,
                        s.Status,
                        s.LastSyncAt,
                        s.ErrorCount,
                        s.LastErrorMessage));
                }
            }
            else
            {
                result.Add(new ExchangeAccountDto(
                    account.Id,
                    account.Name,
                    string.Empty,
                    "NotConfigured",
                    null,
                    0,
                    null));
            }
        }

        return new Response("ok", true, result);
    }
}
