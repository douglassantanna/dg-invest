using api.Cryptos.Models;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Contracts;
public interface ITransactionService
{
    Response ExecuteTransaction(Account account, AccountTransaction accountTransaction);
}
