using api.Data;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;
public record GetUserAccountsQueryCommand(int UserId) : IRequest<Response>;
public class GetUserAccountsQueryCommandHandler : IRequestHandler<GetUserAccountsQueryCommand, Response>
{
    private readonly ILogger<GetUserAccountsQueryCommandHandler> _logger;
    private readonly DataContext _context;

    public GetUserAccountsQueryCommandHandler(
        ILogger<GetUserAccountsQueryCommandHandler> logger,
        DataContext context)
    {
        _logger = logger;
        _context = context;
    }
    public async Task<Response> Handle(GetUserAccountsQueryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetUserAccountsQueryCommandHandler for UserId: {0}", request.UserId);
        try
        {
            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .OrderBy(x => x.IsSelected == true)
                .Select(x => new SimpleAccountDto(x.Id, x.SubaccountTag, x.Balance, x.IsSelected))
                .ToListAsync();
            return new Response("", true, accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserAccountsQueryCommandHandler. Error getting accounts for UserId: {0}; Ex: {1}", request.UserId, ex.Message);
            return new Response("Error getting accounts", false);
        }
    }
}