using api.Cryptos.Models;
using api.Shared;

namespace api.Cryptos.TransactionStrategies;
public interface ITransactionStrategy
{
    Response ExcecuteTransaction(Account account, AccountTransaction accountTransaction);
    EAccountTransactionType TransactionType { get; }
}
