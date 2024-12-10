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