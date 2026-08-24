using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Queries;

public record GetExchangeTransactionsQuery(int UserId, int AccountId, int Limit = 20) : IRequest<Response>;

public record ExchangeTransactionDto(
    int Id,
    DateTime Date,
    string Type,
    string Asset,
    decimal Amount,
    decimal Price,
    decimal Fee,
    string? ExchangeName,
    string? ExchangeStatus,
    string Notes);

public class GetExchangeTransactionsQueryHandler : IRequestHandler<GetExchangeTransactionsQuery, Response>
{
    private readonly DataContext _context;

    public GetExchangeTransactionsQueryHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(GetExchangeTransactionsQuery request, CancellationToken cancellationToken)
    {
        var accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsDeleted
                        && a.AccountType == api.Cryptos.Models.EAccountType.Exchange, cancellationToken);

        if (!accountExists)
            return new Response("Account not found", false, 404);

        var transactions = await _context.AccountTransactions
            .Where(t => EF.Property<int>(t, "AccountId") == request.AccountId
                        && t.ExchangeName != null
                        && t.ExchangeName != "")
            .OrderByDescending(t => t.Date)
            .Take(request.Limit)
            .Select(t => new ExchangeTransactionDto(
                t.Id,
                t.Date,
                t.TransactionType.ToString(),
                t.CryptoAsset != null ? t.CryptoAsset.Symbol : "-",
                t.Amount,
                t.CryptoCurrentPrice,
                t.Fee,
                t.ExchangeName,
                t.ExchangeStatus,
                t.Notes))
            .ToListAsync(cancellationToken);

        return new Response("ok", true, transactions);
    }
}
