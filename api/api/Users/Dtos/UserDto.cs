using api.Cryptos.Models;
using api.Users.Models;

namespace api.Users.Dtos;
public record UserDto(
    int Id,
    string FullName,
    string Email,
    Role Role,
    AccountDto? Account = null
);
public record AccountDto(int Id, decimal Balance, List<GroupedAccountTransactionsDto> GroupedAccountTransactions,
    int TotalCount = 0, int Page = 1, int PageSize = 20, bool HasNextPage = false, bool HasPreviousPage = false);
public record SubAccountDto(int Id, string Name);
public record SimpleAccountDto(int Id, string Name, decimal Balance, bool IsSelected)
{
    public string SubaccountTag => Name;
}
public record AccountTransactionDto(DateTime Date,
                                    EAccountTransactionType TransactionType,
                                    decimal Amount,
                                    string ExchangeName,
                                    string Notes,
                                    decimal CryptoCurrentPrice,
                                    string CryptoSymbol,
                                    decimal? Fee,
                                    string? ExchangeStatus);

public record GroupedAccountTransactionsDto(DateTime Date,
                                            List<AccountTransactionDto> Transactions);
