using api.Cryptos.Models;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Contracts;
public class TransactionService : ITransactionService
{
  private readonly Dictionary<EAccountTransactionType, ITransactionStrategy> _transactionTypes = new();
  public TransactionService(IEnumerable<ITransactionStrategy> transactions)
  {
    foreach (var transaction in transactions)
    {
      _transactionTypes.Add(transaction.TransactionType, transaction);
    }
  }
  public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction)
  {
    if (!_transactionTypes.TryGetValue(accountTransaction.TransactionType, out var transactionType))
    {
      throw new ArgumentNullException("Transaction type not supported", accountTransaction.TransactionType.ToString());
    }
    return transactionType.ExcecuteTransaction(account, accountTransaction);
  }
}