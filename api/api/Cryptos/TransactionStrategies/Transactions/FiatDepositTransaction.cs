using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class FiatDepositTransaction : ITransactionStrategy
{
    public EAccountTransactionType TransactionType => EAccountTransactionType.DepositFiat;

    public Response ExcecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        account.AddToBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);
        return new("Transaction executed successfuly", true);
    }
}
