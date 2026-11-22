using api.AzureKeyVault;
using api.Cache;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitAccountSyncServiceSyncTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task SyncAsync_ProcessesEventsChronologically_WithoutLocalCashMutation()
    {
        var harness = await Harness.CreateAsync();
        var deposit = new BybitDepositWithdrawalRow { TxId = "deposit-1", Coin = "USDT", Amount = "1000", Status = "3", SuccessAt = Ms(-3) };
        var buy = Order("buy-1", "Buy", -2);
        var sell = Order("sell-1", "Sell", -1);
        // Bybit returns order history newest first.
        harness.Orders.AddRange([sell, buy]);
        harness.Deposits.Add(deposit);

        var calls = new List<string>();
        var applyCashFlags = new List<bool>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, _, applyCash) => { calls.Add(order.OrderId); applyCashFlags.Add(applyCash); })
            .ReturnsAsync(true);
        harness.OrderSync.Setup(service => service.ProcessDepositAsync(
                It.IsAny<BybitDepositWithdrawalRow>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .Callback<BybitDepositWithdrawalRow, Account, int, CancellationToken, bool>(
                (row, _, _, _, applyCash) => { calls.Add(row.TxId); applyCashFlags.Add(applyCash); })
            .ReturnsAsync(true);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        calls.Should().Equal("deposit-1", "buy-1", "sell-1");
        applyCashFlags.Should().AllSatisfy(flag => flag.Should().BeFalse());
        harness.LastCheckpointOrderId.Should().Be("sell-1");
    }

    [Fact]
    public async Task SyncAsync_ReconcilesCashBalanceToBybitWallet()
    {
        var harness = await Harness.CreateAsync(localBalance: 468.0960302072m);
        harness.CashBalances[("FUND", "USDT")] = "1000";
        harness.CashBalances[("UNIFIED", "USDC")] = "25.5";
        harness.Orders.Add(Order("buy-1", "Buy", -1));
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        harness.Account.Balance.Should().Be(1025.5m);
        (await harness.Context.Accounts.AsNoTracking().SingleAsync(account => account.Id == harness.Account.Id))
            .Balance.Should().Be(1025.5m);
        harness.LastCheckpointOrderId.Should().Be("buy-1");
    }

    [Fact]
    public async Task SyncAsync_WhenBybitCashBalanceCannotBeRead_KeepsLocalCashBalance()
    {
        var harness = await Harness.CreateAsync(localBalance: 468m);
        harness.CashBalanceError = new InvalidOperationException("balance endpoint unavailable");

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        harness.Account.Balance.Should().Be(468m);
    }

    [Fact]
    public async Task SyncAsync_WithOldCheckpoint_RequestsContiguousSevenDayWindowsUpToNow()
    {
        var lastSyncAt = DateTime.UtcNow.AddDays(-40);
        var harness = await Harness.CreateAsync(lastSyncAt: lastSyncAt);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        AssertContiguousWindows(harness.OrderHistoryRequests, lastSyncAt.AddMinutes(-5));
        harness.DepositHistoryRequests.Should().Equal(harness.OrderHistoryRequests);
        var lastEnd = DateTimeOffset.FromUnixTimeMilliseconds(harness.OrderHistoryRequests[^1].End!.Value);
        lastEnd.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        harness.LastCheckpointAt.Should().BeCloseTo(lastEnd.UtcDateTime, TimeSpan.FromMilliseconds(2));
    }

    [Fact]
    public async Task SyncAsync_WithCheckpointBeyondWindowCap_AdvancesCheckpointOnlyToLastWindow()
    {
        var lastSyncAt = DateTime.UtcNow.AddDays(-100);
        var harness = await Harness.CreateAsync(lastSyncAt: lastSyncAt);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        harness.OrderHistoryRequests.Should().HaveCount(8);
        AssertContiguousWindows(harness.OrderHistoryRequests, lastSyncAt.AddMinutes(-5));
        var lastEnd = DateTimeOffset.FromUnixTimeMilliseconds(harness.OrderHistoryRequests[^1].End!.Value);
        lastEnd.Should().BeBefore(DateTimeOffset.UtcNow.AddDays(-30));
        harness.LastCheckpointAt.Should().BeCloseTo(lastEnd.UtcDateTime, TimeSpan.FromMilliseconds(2));
    }

    [Fact]
    public async Task SyncAsync_WithFreshCredentialsAndNoSync_RequestsOneWindowFromCredentialsSetAt()
    {
        // EnableForCredentials stamps BybitCredentialsSetAt = now, which is the cutoff before a first sync.
        var harness = await Harness.CreateAsync();

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        var request = harness.OrderHistoryRequests.Should().ContainSingle().Subject;
        var start = DateTimeOffset.FromUnixTimeMilliseconds(request.Start!.Value);
        var end = DateTimeOffset.FromUnixTimeMilliseconds(request.End!.Value);
        start.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(-5), TimeSpan.FromMinutes(1));
        end.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SyncAsync_WithoutAnyCheckpoint_LooksBackThirtyDaysInSevenDayWindows()
    {
        var harness = await Harness.CreateAsync(credentialsSetAt: false);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        AssertContiguousWindows(harness.OrderHistoryRequests, DateTime.UtcNow.AddDays(-30));
        harness.OrderHistoryRequests.Should().HaveCount(5);
        harness.DepositHistoryRequests.Should().Equal(harness.OrderHistoryRequests);
        DateTimeOffset.FromUnixTimeMilliseconds(harness.OrderHistoryRequests[^1].End!.Value)
            .Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SyncAsync_WhenCachedCredentialIsRejected_RetriesWithFreshCredentialsBeforePausing()
    {
        // The Functions host caches credentials for 24h, so a key saved through the API after a reconnect
        // is invisible until the cache is cleared. The first call fails with the stale key (10003).
        var harness = await Harness.CreateAsync();
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        harness.Context.ExchangeIntegrations.Add(integration);
        await harness.Context.SaveChangesAsync();
        var orderHistoryCalls = 0;
        harness.Bybit.Setup(s => s.GetOrderHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ReturnsAsync(() => ++orderHistoryCalls == 1
                ? throw new BybitApiException(10003, "API key is invalid.", "/v5/order/history")
                : []);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        orderHistoryCalls.Should().Be(2);
        harness.KeyVault.Verify(vault => vault.InvalidateCachedSecret(It.Is<string>(name => name.EndsWith("api-key"))), Times.AtLeastOnce);
        (await harness.Context.ExchangeIntegrations.SingleAsync()).Status.Should().NotBe(ExchangeIntegration.AutoPausedStatus);
    }

    [Fact]
    public async Task SyncAsync_WhenFreshCredentialIsAlsoRejected_PausesTheIntegration()
    {
        var harness = await Harness.CreateAsync();
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        harness.Context.ExchangeIntegrations.Add(integration);
        await harness.Context.SaveChangesAsync();
        harness.Bybit.Setup(s => s.GetOrderHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10003, "API key is invalid.", "/v5/order/history"));

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await harness.Context.ExchangeIntegrations.SingleAsync()).Status.Should().Be(ExchangeIntegration.AutoPausedStatus);
    }

    [Fact]
    public async Task SyncAsync_WhenKeyCannotReadTransfers_SkipsTransfersAndStillImportsOrders()
    {
        // Sub-account keys need Wallet > SubMemberTransferList to list universal transfers. Without it Bybit returns
        // 10005; that must not stop orders, deposits and withdrawals from being imported.
        var harness = await Harness.CreateAsync();
        harness.Orders.Add(Order("buy-1", "Buy", -1));
        harness.Bybit.Setup(s => s.GetUniversalTransferHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10005, "Permission denied, please check your API key permissions.", "/v5/asset/transfer/query-universal-transfer-list"));
        harness.Bybit.Setup(s => s.GetInternalTransferHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10005, "Permission denied, please check your API key permissions.", "/v5/asset/transfer/query-inter-transfer-list"));
        var processed = new List<string>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, _, _) => processed.Add(order.OrderId))
            .ReturnsAsync(true);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().Equal("buy-1");
        harness.LastCheckpointAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncAsync_WhenAnItemCannotBeImported_SkipsItAdvancesTheCheckpointAndWarns()
    {
        // A permanently failing item (e.g. a coin CoinMarketCap does not know) used to block the account forever.
        var harness = await Harness.CreateAsync();
        harness.Orders.AddRange([Order("bad-1", "Buy", -2), Order("good-1", "Buy", -1)]);
        var processed = new List<string>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, _, _) => processed.Add(order.OrderId))
            .ReturnsAsync((BybitOrderData order, Account _, int _, string _, CancellationToken _, IReadOnlyList<BybitExecutionData>? _, bool _) => order.OrderId != "bad-1");

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().Equal("bad-1", "good-1");
        harness.LastCheckpointAt.Should().NotBeNull("the checkpoint must advance past the skipped item");
        harness.OrderSync.Verify(service => service.MarkSyncStatusWarningAsync(
            harness.Account.UserId, harness.Account.Id, It.Is<string>(message => message.StartsWith("1 Bybit item")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_WhenEveryItemImports_RecordsNoWarning()
    {
        var harness = await Harness.CreateAsync();
        harness.Orders.Add(Order("good-1", "Buy", -1));
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        harness.OrderSync.Verify(service => service.MarkSyncStatusWarningAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenAccountHasNoKey_MarksItNotConfiguredAndClearsAStaleError()
    {
        // Account 36 (STAGE) kept "Credential storage is temporarily unavailable" from an old failure, because a sync
        // skipped for missing credentials never updated the status.
        var harness = await Harness.CreateAsync();
        var status = await harness.Context.SyncStatuses.SingleAsync();
        status.MarkError("Credential storage is temporarily unavailable");
        await harness.Context.SaveChangesAsync();
        harness.KeyVault.Setup(vault => vault.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSkipped.Should().BeTrue();
        status.Status.Should().Be("NotConfigured");
        status.LastErrorMessage.Should().BeNull();
        status.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_ImportsPartiallyFilledCanceledOrdersWithExecutedQuantity()
    {
        var harness = await Harness.CreateAsync();
        var partial = Order("partial-1", "Buy", -2);
        partial.OrderStatus = "PartiallyFilledCanceled";
        partial.CumExecQty = "0.5";
        var unfilled = Order("unfilled-1", "Buy", -1);
        unfilled.OrderStatus = "PartiallyFilledCanceled";
        unfilled.CumExecQty = "0";
        harness.Orders.AddRange([unfilled, partial]);
        var processed = new List<string>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, _, _) => processed.Add(order.OrderId))
            .ReturnsAsync(true);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().Equal("partial-1");
    }

    private static void AssertContiguousWindows(List<(long? Start, long? End)> requests, DateTime expectedStart)
    {
        requests.Should().NotBeEmpty();
        requests.Should().AllSatisfy(request =>
        {
            request.Start.Should().NotBeNull();
            request.End.Should().NotBeNull();
            (request.End!.Value - request.Start!.Value).Should().BeLessThanOrEqualTo((long)TimeSpan.FromDays(7).TotalMilliseconds);
        });
        DateTimeOffset.FromUnixTimeMilliseconds(requests[0].Start!.Value).UtcDateTime
            .Should().BeCloseTo(expectedStart, TimeSpan.FromSeconds(1));
        for (var i = 1; i < requests.Count; i++)
            requests[i].Start.Should().Be(requests[i - 1].End);
    }

    private static string Ms(int minutesFromNow) => Now.AddMinutes(minutesFromNow).ToUnixTimeMilliseconds().ToString();

    private static BybitOrderData Order(string orderId, string side, int minutesFromNow) => new()
    {
        OrderId = orderId,
        Symbol = "BTCUSDT",
        Side = side,
        OrderStatus = "Filled",
        AvgPrice = "100",
        CumExecQty = "1",
        CumExecFee = "0",
        CreatedTime = Ms(minutesFromNow)
    };

    private sealed class Harness
    {
        public required DataContext Context { get; init; }
        public required Account Account { get; init; }
        public required BybitAccountSyncService Service { get; init; }
        public required Mock<IBybitOrderSyncService> OrderSync { get; init; }
        public required Mock<IKeyVaultService> KeyVault { get; init; }
        public required Mock<IBybitService> Bybit { get; init; }
        public List<BybitOrderData> Orders { get; } = [];
        public List<BybitDepositWithdrawalRow> Deposits { get; } = [];
        public Dictionary<(string AccountType, string Coin), string> CashBalances { get; } = [];
        public Exception? CashBalanceError { get; set; }
        public string? LastCheckpointOrderId { get; private set; }
        public DateTime? LastCheckpointAt { get; private set; }
        public List<(long? Start, long? End)> OrderHistoryRequests { get; } = [];
        public List<(long? Start, long? End)> DepositHistoryRequests { get; } = [];

        public static async Task<Harness> CreateAsync(decimal localBalance = 0, DateTime? lastSyncAt = null, bool credentialsSetAt = true)
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new DataContext(options);
            var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
            account.AddToBalance(localBalance);
            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            var syncStatus = new SyncStatus(1, account.Id, "Bybit");
            // Legacy rows created before BybitCredentialsSetAt existed have no credentials timestamp.
            if (credentialsSetAt)
                syncStatus.EnableForCredentials();
            if (lastSyncAt is { } checkpoint)
                syncStatus.MarkConnected(string.Empty, checkpoint);
            context.SyncStatuses.Add(syncStatus);
            await context.SaveChangesAsync();

            var keyVault = new Mock<IKeyVaultService>();
            keyVault.Setup(vault => vault.GetSecretReadResultAsync(It.IsAny<string>()))
                .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "test-credential"));

            var bybit = new Mock<IBybitService>();
            var orderSync = new Mock<IBybitOrderSyncService>();
            var service = new BybitAccountSyncService(
                bybit.Object,
                orderSync.Object,
                keyVault.Object,
                context,
                Mock.Of<ICacheService>(),
                Mock.Of<ICoinMarketCapService>(),
                NullLogger<BybitAccountSyncService>.Instance);

            var harness = new Harness
            {
                Context = context,
                Account = account,
                Service = service,
                OrderSync = orderSync,
                KeyVault = keyVault,
                Bybit = bybit
            };

            bybit.Setup(s => s.GetOrderHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .Callback<string, string, BybitRegion, int?, long?, CancellationToken, long?>(
                    (_, _, _, _, start, _, end) => harness.OrderHistoryRequests.Add((start, end)))
                .ReturnsAsync(() => harness.Orders.ToList());
            bybit.Setup(s => s.GetDepositHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .Callback<string, string, BybitRegion, int?, long?, CancellationToken, long?>(
                    (_, _, _, _, start, _, end) => harness.DepositHistoryRequests.Add((start, end)))
                .ReturnsAsync(() => harness.Deposits.ToList());
            bybit.Setup(s => s.GetWithdrawalHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .ReturnsAsync([]);
            bybit.Setup(s => s.GetInternalTransferHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .ReturnsAsync([]);
            bybit.Setup(s => s.GetUniversalTransferHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .ReturnsAsync([]);
            bybit.Setup(s => s.GetExecutionHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            bybit.Setup(s => s.GetWalletBalanceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new BybitWalletBalanceResponse());
            bybit.Setup(s => s.GetAccountCoinBalancesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new BybitAccountCoinBalancesResponse());
            bybit.Setup(s => s.GetAccountCoinBalanceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((string _, string _, BybitRegion _, string accountType, string coin, string? _) =>
                {
                    if (harness.CashBalanceError is not null)
                        throw harness.CashBalanceError;
                    var amount = harness.CashBalances.GetValueOrDefault((accountType, coin), "0");
                    return Task.FromResult(new BybitAccountCoinBalanceResponse
                    {
                        Result = new BybitAccountCoinBalanceResult
                        {
                            AccountType = accountType,
                            Balance = new BybitAccountCoinBalance { Coin = coin, WalletBalance = amount, TransferBalance = amount }
                        }
                    });
                });

            orderSync.Setup(s => s.UpsertSyncStatusAsync(
                    account.UserId, account.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<DateTime?>()))
                .Callback<int, int, string?, CancellationToken, DateTime?>((_, _, orderId, _, checkpointAt) =>
                {
                    harness.LastCheckpointOrderId = orderId;
                    harness.LastCheckpointAt = checkpointAt;
                })
                .Returns(Task.CompletedTask);

            return harness;
        }
    }
}
