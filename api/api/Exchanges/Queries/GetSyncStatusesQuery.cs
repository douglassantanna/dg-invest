using api.Data;
using api.Exchanges.Models;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetSyncStatusesQuery(int UserId) : IRequest<Response>;

public record SyncStatusDto(
    int AccountId,
    string? AccountName,
    string ExchangeName,
    string Status,
    DateTime? LastSyncAt,
    string? LastOrderId,
    int ErrorCount,
    string? LastErrorMessage);

public class GetSyncStatusesQueryHandler : IRequestHandler<GetSyncStatusesQuery, Response>
{
    private readonly DataContext _context;

    public GetSyncStatusesQueryHandler(DataContext context) => _context = context;

    public async Task<Response> Handle(GetSyncStatusesQuery request, CancellationToken cancellationToken)
    {
        var statuses = await _context.SyncStatuses
            .Where(s => s.UserId == request.UserId)
            .Join(
                _context.Accounts.Where(a => !a.IsDeleted),
                s => s.AccountId,
                a => a.Id,
                (s, a) => new SyncStatusDto(
                    s.AccountId,
                    a.Name,
                    s.ExchangeName,
                    s.Status,
                    s.LastSyncAt,
                    s.LastOrderId,
                    s.ErrorCount,
                    s.LastErrorMessage))
            .ToListAsync(cancellationToken);

        return new Response("ok", true, statuses);
    }
}
