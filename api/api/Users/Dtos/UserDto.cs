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
public record AccountDto(int Id, decimal Balance, List<GroupedAccountTransactionsDto> GroupedAccountTransactions);
public record SubAccountDto(int Id, string Name);
public record SimpleAccountDto(int Id, string SubaccountTag, decimal Balance);
public record AccountTransactionDto(DateTime Date,
                                    EAccountTransactionType TransactionType,
                                    decimal Amount,
                                    string ExchangeName,
                                    string Notes,
                                    decimal CryptoCurrentPrice,
                                    string CryptoSymbol,
                                    decimal? Fee);

public record GroupedAccountTransactionsDto(DateTime Date,
                                            List<AccountTransactionDto> Transactions);