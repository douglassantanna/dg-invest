using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class CryptoDepositTransaction : ITransactionStrategy
{
    private readonly ILogger<CryptoDepositTransaction> _logger;

    public CryptoDepositTransaction(ILogger<CryptoDepositTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.DepositCrypto;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing crypto deposit transaction for account ID: {AccountId}, amount: {Amount}, crypto price: {CryptoPrice}",
            account.Id, accountTransaction.Amount, accountTransaction.CryptoCurrentPrice);

        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        account.AddToBalance(balance);
        account.AddTransaction(accountTransaction);

        _logger.LogInformation("Crypto deposit transaction executed successfully for account ID: {AccountId}, new balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }
}

