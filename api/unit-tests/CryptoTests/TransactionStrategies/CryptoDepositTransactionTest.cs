namespace unit_tests.CryptoTests.TransactionStrategies;

public class CryptoDepositTransactionTest
{
    private readonly CryptoDepositTransaction _sut;
    private readonly Mock<ILogger<CryptoDepositTransaction>> _loggerMock;

    public CryptoDepositTransactionTest()
    {
        _loggerMock = new Mock<ILogger<CryptoDepositTransaction>>();
        _sut = new CryptoDepositTransaction(_loggerMock.Object);
    }

    [Fact]
    public void TransactionType_ShouldBeDepositCrypto()
    {
        _sut.TransactionType.Should().Be(EAccountTransactionType.DepositCrypto);
    }

    [Fact]
    public void ExecuteTransaction_ShouldAddBalanceAndAddTransaction()
    {
        var account = new Account("test", 1);
        account.AddToBalance(5000);

        var cryptoAsset = new CryptoAsset("ETH", "USD", "Ethereum", 1027);
        account.AddCryptoAsset(cryptoAsset);

        var accountTransaction = new AccountTransaction(
            date: DateTime.Now,
            transactionType: EAccountTransactionType.DepositCrypto,
            amount: 2m,
            cryptoCurrentPrice: 3000m,
            exchangeName: "Bybit",
            notes: "test deposit",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: 5m);

        var result = _sut.ExecuteTransaction(account, accountTransaction);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(5000 + (2m * 3000m));
        account.AccountTransactions.Should().Contain(accountTransaction);
    }
}
