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
    public async Task ProcessOpeningBalanceAsync_WhenOnlyOpeningBalanceExists_ShouldReconcileWalletAggregation()
    {
        using var context = CreateContext();
        var account = new Account("Bybit", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);

        await sut.ProcessOpeningBalanceAsync(account, 1, 1_000m, CancellationToken.None);
        var result = await sut.ProcessOpeningBalanceAsync(account, 1, 11_000m, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(11_000m);
        (await context.AccountTransactions.CountAsync()).Should().Be(1);
        (await context.AccountTransactions.SingleAsync()).Amount.Should().Be(11_000m);
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenOrderExistsOnAnotherAccount_ShouldImportForCurrentAccount()
    {
        using var context = CreateContext();
        const string orderId = "order-shared-between-accounts";
        var otherAccount = new Account("Other", 1, EAccountType.Exchange, "Bybit", "UID-OTHER");
        var currentAccount = new Account("Current", 1, EAccountType.Exchange, "Bybit", "UID-CURRENT");
        var otherAsset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        var currentAsset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        otherAsset.AddTransaction(new CryptoTransaction(1m, 1m, DateTimeOffset.UtcNow, "Bybit", ETransactionType.Buy, 0, orderId));
        otherAccount.AddCryptoAsset(otherAsset).IsSuccess.Should().BeTrue();
        currentAccount.AddCryptoAsset(currentAsset).IsSuccess.Should().BeTrue();
        context.Accounts.AddRange(otherAccount, currentAccount);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        await sut.ProcessOpeningBalanceAsync(currentAccount, 1, 100_000m, CancellationToken.None);

        var result = await sut.ProcessOrderAsync(new BybitOrderData
        {
            OrderId = orderId,
            Symbol = "BTCUSDT",
            Side = "Buy",
            OrderStatus = "Filled",
            AvgPrice = "50000",
            CumExecQty = "0.00004",
            CumExecFee = "0",
            CreatedTime = "1790000000000"
        }, currentAccount, 1, "REST", CancellationToken.None,
        [new BybitExecutionData
        {
            OrderId = orderId,
            ExecId = "execution-1",
            ExecFee = "0.000000025",
            FeeCurrency = "BTC",
            ExecPrice = "50000",
            ExecQty = "0.00004"
        }]);

        result.Should().BeTrue();
        var transaction = await context.AccountTransactions
            .SingleAsync(t => t.ExchangeTransactionId == orderId);
        (await context.AccountTransactions
            .Where(t => t.ExchangeTransactionId == orderId)
            .Select(t => EF.Property<int?>(t, "AccountId"))
            .SingleAsync()).Should().Be(currentAccount.Id);
        transaction.Amount.Should().Be(0.00004m);
        transaction.Fee.Should().Be(0.000000025m);
        transaction.FeeCurrency.Should().Be("BTC");
        transaction.FeeQuoteValue.Should().Be(0.00125m);
        currentAccount.Balance.Should().Be(99_997.99875m);
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

    [Fact]
    public async Task ProcessInternalTransferAsync_WhenAccountReceivesStablecoin_ShouldCreateTransferInLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Sub", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "transfer-1",
            Coin = "USDT",
            Amount = "250.50",
            FromAccountType = "FUND",
            ToAccountType = "UNIFIED",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };

        var result = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(250.50m);
        account.TotalDeposited().Should().Be(250.50m);
        var transaction = await context.AccountTransactions.SingleAsync();
        transaction.TransactionType.Should().Be(EAccountTransactionType.TransferIn);
        transaction.ExchangeTransactionId.Should().Be($"bybit-internal-transfer-{transfer.TransferId}-in-{account.Id}");
    }

    [Fact]
    public async Task ProcessInternalTransferAsync_WhenAccountSendsStablecoin_ShouldCreateTransferOutLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Sub", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        await sut.ProcessOpeningBalanceAsync(account, 1, 1_000m, CancellationToken.None);
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "transfer-2",
            Coin = "USDC",
            Amount = "125.50",
            FromAccountType = "UNIFIED",
            ToAccountType = "FUND",
            FromMemberId = "UID-001",
            Timestamp = "1790000000000"
        };

        var result = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(874.50m);
        account.TotalDeposited().Should().Be(874.50m);
        var transaction = await context.AccountTransactions.SingleAsync(t => t.ExchangeTransactionId == $"bybit-internal-transfer-{transfer.TransferId}-out-{account.Id}");
        transaction.TransactionType.Should().Be(EAccountTransactionType.TransferOut);
        transaction.Amount.Should().Be(125.50m);
    }

    [Fact]
    public async Task ProcessInternalTransferAsync_WhenFundTransferTargetsSubaccount_ShouldCreateTransferInLedgerTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Sub", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "transfer-fund-1",
            Coin = "USDC",
            Amount = "10",
            FromAccountType = "FUND",
            ToAccountType = "FUND",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };

        var result = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(10m);
        var transaction = await context.AccountTransactions.SingleAsync();
        transaction.TransactionType.Should().Be(EAccountTransactionType.TransferIn);
        transaction.ExchangeTransactionId.Should().Be($"bybit-internal-transfer-{transfer.TransferId}-in-{account.Id}");
    }

    [Fact]
    public async Task ProcessInternalTransferAsync_WhenCalledTwice_ShouldNotDuplicateTransaction()
    {
        using var context = CreateContext();
        var account = new Account("Sub", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "transfer-3",
            Coin = "USDT",
            Amount = "50",
            ToAccountType = "UNIFIED",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };

        var first = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);
        var second = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeTrue();
        account.Balance.Should().Be(50m);
        (await context.AccountTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcessInternalTransferAsync_WhenTransferOnlyMovesBetweenWallets_ShouldNotChangeAccountBalance()
    {
        using var context = CreateContext();
        var account = new Account("Sub", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var sut = CreateService(context);
        await sut.ProcessOpeningBalanceAsync(account, 1, 100m, CancellationToken.None);
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "wallet-move-1",
            Coin = "USDT",
            Amount = "25",
            FromAccountType = "FUND",
            FromMemberId = "UID-001",
            ToAccountType = "UNIFIED",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };

        var result = await sut.ProcessInternalTransferAsync(transfer, account, 1, CancellationToken.None);

        result.Should().BeTrue();
        account.Balance.Should().Be(100m);
        (await context.AccountTransactions.CountAsync()).Should().Be(1);
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
            new WithdrawDepositTransaction(Mock.Of<ILogger<WithdrawDepositTransaction>>()),
            new TransferInTransaction(Mock.Of<ILogger<TransferInTransaction>>()),
            new TransferOutTransaction(Mock.Of<ILogger<TransferOutTransaction>>()),
            new BuyTransaction(Mock.Of<ILogger<BuyTransaction>>()),
            new SellTransaction(Mock.Of<ILogger<SellTransaction>>())
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
