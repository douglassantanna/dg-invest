using api.Cryptos.Models;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class BuyTransaction : ITransactionStrategy
{
    public EAccountTransactionType TransactionType => EAccountTransactionType.Out;

    public Response ExcecuteTransaction(Account account, AccountTransaction accountTransaction)
    {
        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        if (account.Balance < balance)
        {
            return new("You don't have sufficient funds to complete this transaction", false);
        }

        account.AddTransaction(accountTransaction);
        account.SubtractFromBalance(balance);

        return new("Transaction excecuted successfuly", true);
    }
}
