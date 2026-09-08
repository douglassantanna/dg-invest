using api.AzureStorage;
using api.AzureStorage.Blob;
using api.CoinMarketCap.Service;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Services;

public class BybitOrderSyncServiceTests
{
    [Fact]
    public async Task ProcessOpeningBalanceAsync_WhenCalledTwice_ShouldCreateOneDepositLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Bybit", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);

        var first = await sut.ProcessOpeningBalanceAsync(account, 1, 1_000m, CancellationToken.None);
        var second = await sut.ProcessOpeningBalanceAsync(account, 1, 1_000m, CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeTrue();
        account.Balance.Should().Be(1_000m);
        account.TotalDeposited().Should().Be(1_000m);
        var transaction = await context.AccountTransactions.SingleAsync();
        transaction.TransactionType.Should().Be(EAccountTransactionType.DepositFiat);
        transaction.Amount.Should().Be(1_000m);
        transaction.ExchangeName.Should().Be("Bybit");
        transaction.ExchangeTransactionId.Should().Be($"bybit-opening-balance-{account.Id}");
    }

    [Fact]
    public async Task ProcessDepositAsync_WhenDepositIsStablecoin_ShouldCreateFiatDepositLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Bybit", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        var deposit = new BybitDepositWithdrawalRow
        {
            Coin = "USDT",
            Amount = "500.25",
            Status = "3",
            TxId = "deposit-1",
            SuccessAt = "2026-09-08T12:00:00Z"
        };

        var result = await sut.ProcessDepositAsync(deposit, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(500.25m);
        account.TotalDeposited().Should().Be(500.25m);
        account.CryptoAssets.Should().BeEmpty();
        var transaction = await context.AccountTransactions.SingleAsync();
        transaction.TransactionType.Should().Be(EAccountTransactionType.DepositFiat);
        transaction.Amount.Should().Be(500.25m);
        transaction.ExchangeTransactionId.Should().Be("deposit-1");
    }

    [Fact]
    public async Task ProcessWithdrawalAsync_WhenWithdrawalIsStablecoin_ShouldCreateCashWithdrawalLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Bybit", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        await sut.ProcessOpeningBalanceAsync(account, 1, 1_000m, CancellationToken.None);
        var withdrawal = new BybitDepositWithdrawalRow
        {
            Coin = "USDC",
            Amount = "125.50",
            Status = "success",
            TxId = "withdrawal-1",
            WithdrawFee = "1",
            SuccessAt = "2026-09-08T12:00:00Z"
        };

        var result = await sut.ProcessWithdrawalAsync(withdrawal, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(874.50m);
        account.TotalDeposited().Should().Be(874.50m);
        account.CryptoAssets.Should().BeEmpty();
        var transaction = await context.AccountTransactions.SingleAsync(t => t.ExchangeTransactionId == "withdrawal-1");
        transaction.TransactionType.Should().Be(EAccountTransactionType.WithdrawToBank);
        transaction.Amount.Should().Be(125.50m);
    }

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    private static BybitOrderSyncService CreateService(DataContext context)
    {
        var transactionService = new TransactionService([
            new FiatDepositTransaction(Mock.Of<ILogger<FiatDepositTransaction>>()),
            new WithdrawDepositTransaction(Mock.Of<ILogger<WithdrawDepositTransaction>>())
        ]);
        var blobStorage = new Mock<IBlobStorageService>();
        blobStorage.Setup(x => x.AppendLogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new BybitOrderSyncService(
            Mock.Of<ICoinMarketCapService>(),
            transactionService,
            blobStorage.Object,
            Options.Create(new AzureStorageSettings()),
            context,
            Mock.Of<ILogger<BybitOrderSyncService>>(),
            Mock.Of<ICacheService>());
    }
}
