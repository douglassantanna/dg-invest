using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class WithdrawCryptoTransaction : ITransactionStrategy
{
    private readonly ILogger<WithdrawCryptoTransaction> _logger;

    public WithdrawCryptoTransaction(ILogger<WithdrawCryptoTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.WithdrawCrypto;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing crypto withdrawal transaction for account ID: {AccountId}, amount: {Amount}, crypto price: {CryptoPrice}",
            account.Id, accountTransaction.Amount, accountTransaction.CryptoCurrentPrice);

        var balance = accountTransaction.Amount * accountTransaction.CryptoCurrentPrice;
        account.SubtractFromBalance(balance);
        account.AddTransaction(accountTransaction);

        _logger.LogInformation("Crypto withdrawal transaction executed successfully for account ID: {AccountId}, new balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }
}
