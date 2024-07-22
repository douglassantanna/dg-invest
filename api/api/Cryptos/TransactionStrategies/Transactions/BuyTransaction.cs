using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;

public class BuyTransaction : ITransactionStrategy
{
    private readonly ILogger<BuyTransaction> _logger;

    public BuyTransaction(ILogger<BuyTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.Out;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing transaction for account ID: {AccountId}, transaction amount: {Amount}, current crypto price: {CryptoPrice}",
            account.Id, accountTransaction.Amount, accountTransaction.CryptoCurrentPrice);

        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        if (account.Balance < balance)
        {
            _logger.LogError("BuyTransaction: Insufficient funds for account ID: {AccountId}. Required: {RequiredBalance}, Available: {AvailableBalance}",
                account.Id, balance, account.Balance);
            return new Response("You don't have sufficient funds to complete this transaction", false);
        }

        account.AddTransaction(accountTransaction);
        account.SubtractFromBalance(balance);

        _logger.LogInformation("Transaction executed successfully for account ID: {AccountId}. New balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }
}
