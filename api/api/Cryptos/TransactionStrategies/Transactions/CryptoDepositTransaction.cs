using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class CryptoDepositTransaction : ITransactionStrategy
{
    public EAccountTransactionType TransactionType => EAccountTransactionType.DepositCrypto;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        account.AddToBalance(balance);
        account.AddTransaction(accountTransaction);
        return new("Transaction executed successfuly", true);
    }
}
