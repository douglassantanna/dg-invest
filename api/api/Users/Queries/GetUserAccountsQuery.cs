using api.Data;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;
public record GetUserAccountsQuery(int UserId) : IRequest<Response>;
public class GetUserAccountsQueryHandler : IRequestHandler<GetUserAccountsQuery, Response>
{
    private readonly ILogger<GetUserAccountsQueryHandler> _logger;
    private readonly DataContext _context;

    public GetUserAccountsQueryHandler(
        ILogger<GetUserAccountsQueryHandler> logger,
        DataContext context)
    {
        _logger = logger;
        _context = context;
    }

    // Not cached: a small query, and a cached list went stale after selecting or creating an account.
    public async Task<Response> Handle(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId && !x.IsDeleted)
                .OrderByDescending(x => x.IsSelected == true)
                .Select(x => new SimpleAccountDto(x.Id, x.Name, x.Balance, x.IsSelected,
                    x.Exchange == "Bybit"))
                .ToListAsync(cancellationToken);

            return new Response("", true, accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserAccountsQueryHandler. Error getting accounts for UserId: {0}; Ex: {1}", request.UserId, ex.Message);
            return new Response("Error getting accounts", false);
        }
    }
}
