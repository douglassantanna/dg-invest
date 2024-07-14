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
public record AccountDto(int Id, decimal Balance, List<AccountTransactionDto> AccountTransactions);
public record AccountTransactionDto(DateTime Date,
                                    EAccountTransactionType TransactionType,
                                    decimal Amount,
                                    string ExchangeName,
                                    string Currency,
                                    string Destination,
                                    string Notes,
                                    decimal CryptoCurrentPrice);