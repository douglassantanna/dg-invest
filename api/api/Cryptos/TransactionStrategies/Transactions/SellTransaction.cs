using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class SellTransaction : ITransactionStrategy
{
    public EAccountTransactionType TransactionType => EAccountTransactionType.In;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        if (cryptoAsset?.Balance < accountTransaction.Amount)
        {
            return new Response("You don't have sufficient crypto holdings to complete this transaction", false);
        }

        account.AddTransaction(accountTransaction);

        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        account.AddToBalance(balance);

        return new("Transaction executed successfuly", true);
    }
}
