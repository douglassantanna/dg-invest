using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class SellTransaction : ITransactionStrategy
{
    private readonly ILogger<SellTransaction> _logger;

    public SellTransaction(ILogger<SellTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.In;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
         if (cryptoAsset == null)
        {
            _logger.LogError("SellTransaction: No crypto asset provided for account ID: {AccountId}.", account.Id);
            return new Response("Crypto asset not found. Cannot execute transaction.", false);
        }

        _logger.LogInformation("Executing sell transaction for account ID: {AccountId}, amount: {Amount}, crypto price: {CryptoPrice}",
            account.Id, accountTransaction.Amount, accountTransaction.CryptoCurrentPrice);

        if (cryptoAsset.Balance < accountTransaction.Amount)
        {
            _logger.LogError("SellTransaction: Insufficient crypto holdings for account ID: {AccountId}. Required: {RequiredAmount}, Available: {AvailableAmount}",
                account.Id, accountTransaction.Amount, cryptoAsset?.Balance);

            return new Response("You don't have sufficient crypto holdings to complete this transaction", false);
        }

        account.AddTransaction(accountTransaction);

        decimal totalBalance = CalculateTransactionValue(accountTransaction.Amount, accountTransaction.CryptoCurrentPrice, accountTransaction.Fee);
        account.AddToBalance(totalBalance);

        _logger.LogInformation("Sell transaction executed successfully for account ID: {AccountId}, new balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }

    private decimal CalculateTransactionValue(decimal amount, decimal cryptoPrice, decimal fee)
    {
        return (amount * cryptoPrice) - fee;
    }
}
