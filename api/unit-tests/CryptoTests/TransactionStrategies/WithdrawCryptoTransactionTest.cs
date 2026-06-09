namespace unit_tests.CryptoTests.TransactionStrategies;

public class WithdrawCryptoTransactionTest
{
    private readonly WithdrawCryptoTransaction _sut;
    private readonly Mock<ILogger<WithdrawCryptoTransaction>> _loggerMock;

    public WithdrawCryptoTransactionTest()
    {
        _loggerMock = new Mock<ILogger<WithdrawCryptoTransaction>>();
        _sut = new WithdrawCryptoTransaction(_loggerMock.Object);
    }

    [Fact]
    public void TransactionType_ShouldBeWithdrawCrypto()
    {
        _sut.TransactionType.Should().Be(EAccountTransactionType.WithdrawCrypto);
    }

    [Fact]
    public void ExecuteTransaction_ShouldSubtractBalanceAndAddTransaction()
    {
        var account = new Account("test", 1);
        var cryptoAsset = new CryptoAsset("BTC", "USD", "Bitcoin", 1);
        account.AddCryptoAsset(cryptoAsset);
        account.AddToBalance(10000);

        var accountTransaction = new AccountTransaction(
            date: DateTime.Now,
            transactionType: EAccountTransactionType.WithdrawCrypto,
            amount: 0.5m,
            cryptoCurrentPrice: 50000m,
            exchangeName: "Bybit",
            notes: "test withdrawal",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: 10m);

        var result = _sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(10000 - (0.5m * 50000m));
        account.AccountTransactions.Should().Contain(accountTransaction);
    }

    [Fact]
    public void ExecuteTransaction_ShouldLogInformation()
    {
        var account = new Account("test", 1);
        var cryptoAsset = new CryptoAsset("BTC", "USD", "Bitcoin", 1);
        account.AddCryptoAsset(cryptoAsset);

        var accountTransaction = new AccountTransaction(
            date: DateTime.Now,
            transactionType: EAccountTransactionType.WithdrawCrypto,
            amount: 1m,
            cryptoCurrentPrice: 100m,
            exchangeName: "Test",
            notes: "test",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: 0);

        var result = _sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeTrue();
    }
}
