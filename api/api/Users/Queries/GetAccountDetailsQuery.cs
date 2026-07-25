using api.Cryptos.Models;
using api.Data;
using api.Shared;
using api.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Queries;
public record GetAccountDetailsQuery(int UserId, int Page = 1, int PageSize = 20, string? Filter = null) : IRequest<Response>;
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

        var transactionsQuery = account.AccountTransactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            var filter = request.Filter.ToLower();
            transactionsQuery = transactionsQuery.Where(at =>
                (at.ExchangeName != null && at.ExchangeName.ToLower().Contains(filter)) ||
                (at.Notes != null && at.Notes.ToLower().Contains(filter)) ||
                (at.CryptoAsset != null && at.CryptoAsset.Symbol.ToLower().Contains(filter)) ||
                GetTransactionTypeLabel(at.TransactionType).ToLower().Contains(filter)
            );
        }

        var sortedTransactions = transactionsQuery
            .OrderByDescending(at => at.Date)
            .ToList();

        var totalCount = sortedTransactions.Count;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var pagedTransactions = sortedTransactions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var groupedTransactions = pagedTransactions
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
                    at.Fee,
                    at.ExchangeStatus
                )).ToList()
            ))
            .OrderByDescending(g => g.Date)
            .ToList();

        var accountDto = new AccountDto(
            account.Id,
            account.Balance,
            groupedTransactions,
            totalCount,
            page,
            pageSize,
            page * pageSize < totalCount,
            page > 1
        );

        return new Response("", true, accountDto);
    }

    private static string GetTransactionTypeLabel(EAccountTransactionType type)
    {
        return type switch
        {
            EAccountTransactionType.DepositFiat => "Deposit",
            EAccountTransactionType.DepositCrypto => "Deposit Crypto",
            EAccountTransactionType.WithdrawToBank => "Withdraw to Bank",
            EAccountTransactionType.In => "Sell",
            EAccountTransactionType.Out => "Buy",
            EAccountTransactionType.WithdrawCrypto => "Withdraw Crypto",
            _ => ""
        };
    }
}