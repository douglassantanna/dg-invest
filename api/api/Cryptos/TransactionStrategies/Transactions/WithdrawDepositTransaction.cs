using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class WithdrawDepositTransaction : ITransactionStrategy
{
    public EAccountTransactionType TransactionType => EAccountTransactionType.WithdrawToBank;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        if (account.Balance < accountTransaction.Amount)
        {
            return new Response("You don't have sufficient fund to complete the withdraw", false);
        }
        account.SubtractFromBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);
        return new("Transaction executed successfuly", true);
    }
}
