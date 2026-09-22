namespace unit_tests.CryptoTests.TransactionStrategies;

public class TransferTransactionTests
{
    [Fact]
    public void TransferInTransaction_WhenExecuted_ShouldIncreaseBalanceAndAddTransaction()
    {
        var sut = new TransferInTransaction(Mock.Of<ILogger<TransferInTransaction>>());
        var account = new Account("test", 1);
        var accountTransaction = new AccountTransaction(DateTime.UtcNow, EAccountTransactionType.TransferIn, 250m, "Transfer in");

        var result = sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeTrue();
        sut.TransactionType.Should().Be(EAccountTransactionType.TransferIn);
        account.Balance.Should().Be(250m);
        account.AccountTransactions.Should().Contain(accountTransaction);
    }

    [Fact]
    public void TransferOutTransaction_WhenAccountHasEnoughBalance_ShouldDecreaseBalanceAndAddTransaction()
    {
        var sut = new TransferOutTransaction(Mock.Of<ILogger<TransferOutTransaction>>());
        var account = new Account("test", 1);
        account.AddToBalance(300m);
        var accountTransaction = new AccountTransaction(DateTime.UtcNow, EAccountTransactionType.TransferOut, 125m, "Transfer out");

        var result = sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeTrue();
        sut.TransactionType.Should().Be(EAccountTransactionType.TransferOut);
        account.Balance.Should().Be(175m);
        account.AccountTransactions.Should().Contain(accountTransaction);
    }

    [Fact]
    public void TransferOutTransaction_WhenAccountHasInsufficientBalance_ShouldFail()
    {
        var sut = new TransferOutTransaction(Mock.Of<ILogger<TransferOutTransaction>>());
        var account = new Account("test", 1);
        account.AddToBalance(50m);
        var accountTransaction = new AccountTransaction(DateTime.UtcNow, EAccountTransactionType.TransferOut, 125m, "Transfer out");

        var result = sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeFalse();
        account.Balance.Should().Be(50m);
        account.AccountTransactions.Should().BeEmpty();
    }
}
