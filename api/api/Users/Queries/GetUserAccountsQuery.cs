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
    public async Task<Response> Handle(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetUserAccountsQueryHandler for UserId: {0}", request.UserId);
        try
        {
            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.IsSelected == true)
                .Select(x => new SimpleAccountDto(x.Id, x.SubaccountTag, x.Balance, x.IsSelected))
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