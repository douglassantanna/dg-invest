using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;

public class TransferInTransaction : ITransactionStrategy
{
    private readonly ILogger<TransferInTransaction> _logger;

    public TransferInTransaction(ILogger<TransferInTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.TransferIn;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing transfer in transaction for account ID: {AccountId}, amount: {Amount}",
            account.Id, accountTransaction.Amount);

        account.AddToBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);

        return new Response("Transaction executed successfully", true);
    }
}
