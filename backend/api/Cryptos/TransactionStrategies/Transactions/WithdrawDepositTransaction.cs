using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.TransactionStrategies.Transactions;
public class WithdrawDepositTransaction : ITransactionStrategy
{
    private readonly ILogger<WithdrawDepositTransaction> _logger;

    public WithdrawDepositTransaction(ILogger<WithdrawDepositTransaction> logger)
    {
        _logger = logger;
    }

    public EAccountTransactionType TransactionType => EAccountTransactionType.WithdrawToBank;

    public Response ExecuteTransaction(Account account, AccountTransaction accountTransaction, CryptoAsset? cryptoAsset = null)
    {
        _logger.LogInformation("Executing withdraw transaction for account ID: {AccountId}, amount: {Amount}",
            account.Id, accountTransaction.Amount);

        if (account.Balance < accountTransaction.Amount)
        {
            _logger.LogError("WithdrawDepositTransaction: Insufficient funds for account ID: {AccountId}. Required: {RequiredAmount}, Available: {AvailableBalance}",
                account.Id, accountTransaction.Amount, account.Balance);

            return new Response("You don't have sufficient funds to complete the withdraw", false);
        }

        account.SubtractFromBalance(accountTransaction.Amount);
        account.AddTransaction(accountTransaction);

        _logger.LogInformation("Withdraw transaction executed successfully for account ID: {AccountId}, new balance: {NewBalance}",
            account.Id, account.Balance);

        return new Response("Transaction executed successfully", true);
    }
}
