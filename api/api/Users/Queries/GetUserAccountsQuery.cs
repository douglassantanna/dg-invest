using api.Cache;
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
    private readonly ICacheService _cacheService;

    public GetUserAccountsQueryHandler(
        ILogger<GetUserAccountsQueryHandler> logger,
        DataContext context,
        ICacheService cacheService)
    {
        _logger = logger;
        _context = context;
        _cacheService = cacheService;
    }
    public async Task<Response> Handle(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"{CacheKeyConstants.UserAccounts}{request.UserId}";
            var absoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var accounts = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
            {
                return await _context.Accounts
                    .AsNoTracking()
                    .Where(x => x.UserId == request.UserId)
                    .OrderByDescending(x => x.IsSelected == true)
                    .Select(x => new SimpleAccountDto(x.Id, x.SubaccountTag, x.Balance, x.IsSelected))
                    .ToListAsync(ct);
            },
            absoluteExpirationRelativeToNow,
            cancellationToken);

            return new Response("", true, accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserAccountsQueryHandler. Error getting accounts for UserId: {0}; Ex: {1}", request.UserId, ex.Message);
            return new Response("Error getting accounts", false);
        }
    }
}