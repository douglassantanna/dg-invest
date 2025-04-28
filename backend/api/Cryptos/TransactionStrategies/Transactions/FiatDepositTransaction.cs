using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class FiatDepositTransaction : ITransactionStrategy
{
    private readonly ILogger<FiatDepositTransaction> _logger;

    public FiatDepositTransaction(ILogger<FiatDepositTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.DepositFiat;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing fiat deposit transaction for account ID: {AccountId}, amount: {Amount}",
            account.Id, accountTransaction.Amount);

        account.AddToBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);

        _logger.LogInformation("Fiat deposit transaction executed successfully for account ID: {AccountId}, new balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }
}
