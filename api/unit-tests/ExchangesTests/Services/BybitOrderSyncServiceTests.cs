using api.AzureStorage;
using api.AzureStorage.Blob;
using api.CoinMarketCap.Service;
using api.Exchanges.Services;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Services;

public class BybitOrderSyncServiceTests
{
    [Fact]
    public async Task ProcessOpeningBalanceAsync_WhenCalledTwice_ShouldCreateOneDepositLedgerTransaction()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Bybit", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var transactionService = new TransactionService([
            new FiatDepositTransaction(Mock.Of<ILogger<FiatDepositTransaction>>())
        ]);
        var sut = new BybitOrderSyncService(
            Mock.Of<ICoinMarketCapService>(),
            transactionService,
            Mock.Of<IBlobStorageService>(),
            Options.Create(new AzureStorageSettings()),
            context,
            Mock.Of<ILogger<BybitOrderSyncService>>(),
            Mock.Of<ICacheService>());

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
}
