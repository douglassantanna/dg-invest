using api.Data;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;
public record GetAccountDetailsQuery(int UserId) : IRequest<Response>;
public class GetAccountDetailsQueryHandler : IRequestHandler<GetAccountDetailsQuery, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<GetAccountDetailsQueryHandler> _logger;

    public GetAccountDetailsQueryHandler(
        DataContext context,
        ILogger<GetAccountDetailsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAccountDetailsQueryHandler: Handling request for user {UserId}", request.UserId);
        var account = await _context.Accounts
                                    .AsNoTracking()
                                    .Where(u => u.UserId == request.UserId)
                                    .Where(x => x.IsSelected == true)
                                    .Include(x => x.AccountTransactions)
                                    .ThenInclude(x => x.CryptoAsset)
                                    .FirstOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            _logger.LogError("GetAccountDetailsQueryHandler: Account not found for user {UserId}", request.UserId);
            return new Response("Account not found", false, 404);
        }

        var groupedTransactions = account.AccountTransactions
                                        .GroupBy(at => at.Date.Date)
                                        .Select(g => new GroupedAccountTransactionsDto(
                                            g.Key,
                                            g.Select(at => new AccountTransactionDto(
                                                at.Date,
                                                at.TransactionType,
                                                at.Amount,
                                                at.ExchangeName,
                                                at.Notes,
                                                at.CryptoCurrentPrice,
                                                at.CryptoAsset?.Symbol.ToLower() ?? "",
                                                at.Fee
                                            )).ToList()
                                        ))
                                        .OrderByDescending(g => g.Date)
                                        .ToList();

        var accountDto = new AccountDto(
            account.Id,
            account.Balance,
            groupedTransactions
        );

        return new Response("", true, accountDto);
    }
}