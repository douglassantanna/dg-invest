using api.Data;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;
public record GetAccountBySubAccountTagCommand(int UserId, string SubAccountTag) : IRequest<Response>;
public class GetAccountBySubAccountTagCommandHandler : IRequestHandler<GetAccountBySubAccountTagCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<GetAccountBySubAccountTagCommandHandler> _logger;

    public GetAccountBySubAccountTagCommandHandler(
        DataContext context,
        ILogger<GetAccountBySubAccountTagCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(GetAccountBySubAccountTagCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAccountBySubAccountTagCommandHandler: Handling request for user {UserId}", request.UserId);
        var account = await _context.Accounts
                                    .Where(u => u.UserId == request.UserId)
                                    .Where(a => a.SubaccountTag == request.SubAccountTag)
                                    .Include(x => x.AccountTransactions)
                                    .FirstOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            _logger.LogError("GetAccountBySubAccountTagCommandHandler: Account not found for user {UserId}", request.UserId);
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
                                                at.CryptoAsset?.Symbol ?? "",
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