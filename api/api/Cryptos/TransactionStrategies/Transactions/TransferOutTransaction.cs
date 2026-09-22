using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;

public class TransferOutTransaction : ITransactionStrategy
{
    private readonly ILogger<TransferOutTransaction> _logger;

    public TransferOutTransaction(ILogger<TransferOutTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.TransferOut;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing transfer out transaction for account ID: {AccountId}, amount: {Amount}",
            account.Id, accountTransaction.Amount);

        if (account.Balance < accountTransaction.Amount)
        {
            _logger.LogError("TransferOutTransaction: Insufficient funds for account ID: {AccountId}. Required: {RequiredAmount}, Available: {AvailableBalance}",
                account.Id, accountTransaction.Amount, account.Balance);

            return new Response("You don't have sufficient funds to complete the transfer", false);
        }

        account.SubtractFromBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);

        return new Response("Transaction executed successfully", true);
    }
}
