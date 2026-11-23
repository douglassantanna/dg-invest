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

    [Fact]
    public async Task SyncAsync_ReadsTheWalletCashOnceAndUsesItForTheOpeningBalanceAndReconciliation()
    {
        var harness = await Harness.CreateAsync();
        harness.CashBalances[("FUND", "USDT")] = "1000";
        harness.CashBalances[("UNIFIED", "USDC")] = "25.5";

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        // FUND and UNIFIED x USDT and USDC: one read of each per run.
        harness.Bybit.Verify(s => s.GetAccountCoinBalanceAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Exactly(4));
        harness.OrderSync.Verify(s => s.ProcessOpeningBalanceAsync(harness.Account, harness.Account.UserId, 1025.5m, It.IsAny<CancellationToken>()), Times.Once);
        harness.Account.Balance.Should().Be(1025.5m);
    }

    [Fact]
    public async Task SyncAsync_DoesNotFetchExecutionsForAnOrderAlreadyImportedWithItsExecution()
    {
        var harness = await Harness.CreateAsync();
        var asset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        harness.Account.AddCryptoAsset(asset);
        asset.AddHistoricalTransaction(new CryptoTransaction(1, 100, Now.AddMinutes(-1), "Bybit", ETransactionType.Buy, 0,
            exchangeOrderId: "imported-1", exchangeExecutionId: "exec-1"));
        await harness.Context.SaveChangesAsync();
        harness.Orders.AddRange([Order("new-1", "Buy", -1), Order("imported-1", "Buy", -1)]);
        var executionsPassed = new Dictionary<string, IReadOnlyList<BybitExecutionData>?>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, executions, _) => executionsPassed[order.OrderId] = executions)
            .ReturnsAsync(true);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        harness.Bybit.Verify(s => s.GetExecutionHistoryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), "new-1", It.IsAny<CancellationToken>()), Times.Once);
        harness.Bybit.Verify(s => s.GetExecutionHistoryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), "imported-1", It.IsAny<CancellationToken>()), Times.Never);
        // Null tells the order sync not to touch the stored fee.
        executionsPassed["imported-1"].Should().BeNull();
        executionsPassed["new-1"].Should().NotBeNull();
    }

    [Fact]
    public async Task SyncAsync_WhenBybitRateLimits_ReturnsRateLimitedWithoutMarkingTheAccountAsFailed()
    {
        var harness = await Harness.CreateAsync();
        harness.Bybit.Setup(s => s.GetOrderHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10006, "Too many visits"));

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.FailureKind.Should().Be(BybitApiFailureKind.RateLimited);
        result.Message.Should().Be(BybitErrorMessages.RateLimited);
        harness.OrderSync.Verify(s => s.MarkSyncStatusErrorAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.OrderSync.Verify(s => s.UpsertSyncStatusAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenTheCashReadIsRateLimited_ReturnsRateLimitedAndKeepsTheCheckpoint()
    {
        var harness = await Harness.CreateAsync();
        harness.CashBalanceError = new BybitRateLimitException("/v5/asset/transfer/query-account-coin-balance", 429, new Exception("throttled"));

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.FailureKind.Should().Be(BybitApiFailureKind.RateLimited);
        harness.OrderSync.Verify(s => s.UpsertSyncStatusAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenBybitRejectsTheRequest_RecordsAFriendlyMessageWithTheCode()
    {
        var harness = await Harness.CreateAsync();
        harness.Bybit.Setup(s => s.GetOrderHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(131002, "Parameter error"));

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.Message.Should().Contain("date range").And.Contain("[Bybit 131002]");
        harness.OrderSync.Verify(s => s.MarkSyncStatusErrorAsync(
            harness.Account.UserId, harness.Account.Id, result.Message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ImportsALimitOrderThatFilledAfterItsCreationWindow()
    {
        // The AAVE case: created before the window (so order history no longer returns it), filled inside it.
        var harness = await Harness.CreateAsync();
        harness.Fills.AddRange([
            Fill("late-1", "exec-2", "0.0203", "99.33", leavesQty: "0", minutesFromNow: -1),
            Fill("late-1", "exec-1", "0.01", "98", leavesQty: "0.0203", minutesFromNow: -2)]);
        harness.OrdersById["late-1"] = LateOrder("late-1", cumExecQty: "0.0303", avgPrice: "98.9", createdMinutesFromNow: -9, updatedMinutesFromNow: -1);
        var processed = CaptureProcessedOrders(harness);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        var (order, executions) = processed.Single();
        // Quantity and price are Bybit's own order totals, not a rebuild from the fills.
        order.OrderId.Should().Be("late-1");
        order.Symbol.Should().Be("AAVEUSDT");
        order.OrderStatus.Should().Be("Filled");
        order.CumExecQty.Should().Be("0.0303");
        order.AvgPrice.Should().Be("98.9");
        // Dated by its last fill, not by when it was placed.
        order.CreatedTime.Should().Be(Ms(-1));
        executions.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAsync_WhenTheFillsDoNotAddUpToTheOrder_UsesTheOrderFeesAndUpdateTime()
    {
        // Bybit can return only the last 7 days of fills for an order; a partial list must not drive the import.
        var harness = await Harness.CreateAsync();
        harness.Fills.Add(Fill("late-1", "exec-9", "0.01", "99", leavesQty: "0", minutesFromNow: -1));
        harness.OrdersById["late-1"] = LateOrder("late-1", cumExecQty: "0.05", avgPrice: "97.5", createdMinutesFromNow: -20_000, updatedMinutesFromNow: -1);
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        var (order, executions) = processed.Single();
        order.CumExecQty.Should().Be("0.05");
        order.CreatedTime.Should().Be(Ms(-1));
        // Empty fills: the order's own fee details are used instead of a partial sum.
        executions.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_WhenALateOrderIsNotFoundById_RemembersItAsPending()
    {
        var harness = await Harness.CreateAsync();
        harness.Fills.Add(Fill("late-1", "exec-1", "0.01", "99", leavesQty: "0", minutesFromNow: -1));
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().BeEmpty();
        (await PendingOrderIdsAsync(harness)).Should().Equal("late-1");
    }

    private static BybitOrderData LateOrder(string orderId, string cumExecQty, string avgPrice, int createdMinutesFromNow, int updatedMinutesFromNow) => new()
    {
        OrderId = orderId,
        Symbol = "AAVEUSDT",
        Side = "Buy",
        OrderStatus = "Filled",
        CumExecQty = cumExecQty,
        AvgPrice = avgPrice,
        CumExecFee = "0",
        CreatedTime = Ms(createdMinutesFromNow),
        UpdatedTime = Ms(updatedMinutesFromNow)
    };

    [Fact]
    public async Task SyncAsync_DoesNotImportAnOrderThatIsStillPartlyOpen()
    {
        var harness = await Harness.CreateAsync();
        harness.Fills.Add(Fill("open-1", "exec-1", "0.01", "98", leavesQty: "0.02", minutesFromNow: -1));
        var stillOpen = LateOrder("open-1", cumExecQty: "0.01", avgPrice: "98", createdMinutesFromNow: -9, updatedMinutesFromNow: -1);
        stillOpen.OrderStatus = "PartiallyFilled";
        harness.OrdersById["open-1"] = stillOpen;
        var processed = CaptureProcessedOrders(harness);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().BeEmpty();
        // Remembered, so a later cancel of the rest is not missed.
        (await PendingOrderIdsAsync(harness)).Should().Equal("open-1");
    }

    [Fact]
    public async Task SyncAsync_RemembersAPartlyFilledOrderReturnedByOrderHistory()
    {
        var harness = await Harness.CreateAsync();
        var partial = Order("partial-open-1", "Buy", -1);
        partial.OrderStatus = "PartiallyFilled";
        partial.CumExecQty = "0.5";
        harness.Orders.Add(partial);
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().BeEmpty();
        (await PendingOrderIdsAsync(harness)).Should().Equal("partial-open-1");
    }

    [Fact]
    public async Task SyncAsync_ImportsAPendingOrderCancelledAfterAPartialFill()
    {
        var harness = await Harness.CreateAsync();
        await AddPendingAsync(harness, "pending-1", DateTime.UtcNow.AddHours(-3));
        var cancelled = Order("pending-1", "Buy", -180);
        cancelled.OrderStatus = "PartiallyFilledCanceled";
        cancelled.CumExecQty = "0.4";
        harness.OrdersById["pending-1"] = cancelled;
        var processed = CaptureProcessedOrders(harness);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().ContainSingle().Which.Order.Should().BeSameAs(cancelled);
        (await PendingOrderIdsAsync(harness)).Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_LeavesAPendingOrderThatIsStillOpen()
    {
        var harness = await Harness.CreateAsync();
        await AddPendingAsync(harness, "pending-1", DateTime.UtcNow.AddHours(-3));
        harness.OpenOrders.Add(new BybitOrderData { OrderId = "pending-1", OrderStatus = "PartiallyFilled" });
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().BeEmpty();
        (await PendingOrderIdsAsync(harness)).Should().Equal("pending-1");
        harness.Bybit.Verify(s => s.GetOrderAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_DropsPendingOrdersThatAreImportedCancelledWithoutFillsOrTooOld()
    {
        var harness = await Harness.CreateAsync();
        var asset = new CryptoAsset("Aave", "Aave", "AAVE", 7278);
        harness.Account.AddCryptoAsset(asset);
        asset.AddHistoricalTransaction(new CryptoTransaction(1, 100, Now.AddHours(-1), "Bybit", ETransactionType.Buy, 0, exchangeOrderId: "imported-1"));
        await harness.Context.SaveChangesAsync();
        await AddPendingAsync(harness, "imported-1", DateTime.UtcNow.AddHours(-2));
        await AddPendingAsync(harness, "cancelled-empty-1", DateTime.UtcNow.AddHours(-2));
        await AddPendingAsync(harness, "lost-1", DateTime.UtcNow.AddDays(-31));
        await AddPendingAsync(harness, "not-found-yet-1", DateTime.UtcNow.AddDays(-1));
        harness.OrdersById["cancelled-empty-1"] = new BybitOrderData { OrderId = "cancelled-empty-1", OrderStatus = "Cancelled", CumExecQty = "0" };
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().BeEmpty();
        // Not found for 30 days: given up (left to the historical import). Not found for a day: kept.
        (await PendingOrderIdsAsync(harness)).Should().Equal("not-found-yet-1");
        harness.Bybit.Verify(s => s.GetOrderAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), "imported-1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WithNoPendingOrders_DoesNotListOpenOrders()
    {
        var harness = await Harness.CreateAsync();

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        harness.Bybit.Verify(s => s.GetOpenOrdersAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task AddPendingAsync(Harness harness, string orderId, DateTime firstSeenAt)
    {
        harness.Context.BybitPendingOrders.Add(new BybitPendingOrder(harness.Account.Id, orderId, firstSeenAt));
        await harness.Context.SaveChangesAsync();
    }

    private static async Task<List<string>> PendingOrderIdsAsync(Harness harness) =>
        await harness.Context.BybitPendingOrders.AsNoTracking()
            .Where(pending => pending.AccountId == harness.Account.Id)
            .OrderBy(pending => pending.OrderId)
            .Select(pending => pending.OrderId)
            .ToListAsync();

    [Fact]
    public async Task SyncAsync_UsesOrderHistoryForOrdersItReturns()
    {
        var harness = await Harness.CreateAsync();
        var fromHistory = Order("buy-1", "Buy", -2);
        harness.Orders.Add(fromHistory);
        harness.Fills.Add(Fill("buy-1", "exec-1", "1", "100", leavesQty: "0", minutesFromNow: -2));
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().ContainSingle().Which.Order.Should().BeSameAs(fromHistory);
    }

    [Fact]
    public async Task SyncAsync_DoesNotLookUpALateFilledOrderThatIsAlreadyImported()
    {
        var harness = await Harness.CreateAsync();
        var asset = new CryptoAsset("Aave", "Aave", "AAVE", 7278);
        harness.Account.AddCryptoAsset(asset);
        asset.AddHistoricalTransaction(new CryptoTransaction(0.0303m, 98.83m, Now.AddMinutes(-1), "Bybit", ETransactionType.Buy, 0,
            exchangeOrderId: "late-1", exchangeExecutionId: "exec-1"));
        await harness.Context.SaveChangesAsync();
        harness.Fills.Add(Fill("late-1", "exec-1", "0.0303", "98.83", leavesQty: "0", minutesFromNow: -1));
        var processed = CaptureProcessedOrders(harness);

        await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        processed.Should().BeEmpty();
        harness.Bybit.Verify(s => s.GetExecutionHistoryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), "late-1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenTheFillListIsRejected_StillImportsTheRest()
    {
        var harness = await Harness.CreateAsync();
        harness.Orders.Add(Order("buy-1", "Buy", -1));
        harness.Bybit.Setup(s => s.GetExecutionsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BybitApiException(10005, "Permission denied"));
        var processed = CaptureProcessedOrders(harness);

        var result = await harness.Service.SyncAsync(harness.Account, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        processed.Should().ContainSingle().Which.Order.OrderId.Should().Be("buy-1");
    }

    private static List<(BybitOrderData Order, IReadOnlyList<BybitExecutionData>? Executions)> CaptureProcessedOrders(Harness harness)
    {
        var processed = new List<(BybitOrderData, IReadOnlyList<BybitExecutionData>?)>();
        harness.OrderSync.Setup(service => service.ProcessOrderAsync(
                It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyList<BybitExecutionData>?>(), It.IsAny<bool>()))
            .Callback<BybitOrderData, Account, int, string, CancellationToken, IReadOnlyList<BybitExecutionData>?, bool>(
                (order, _, _, _, _, executions, _) => processed.Add((order, executions)))
            .ReturnsAsync(true);
        return processed;
    }

    private static BybitExecutionData Fill(string orderId, string execId, string quantity, string price, string leavesQty, int minutesFromNow) => new()
    {
        OrderId = orderId,
        ExecId = execId,
        Symbol = "AAVEUSDT",
        Side = "Buy",
        ExecQty = quantity,
        ExecPrice = price,
        ExecFee = "0",
        FeeCurrency = "AAVE",
        LeavesQty = leavesQty,
        ExecTime = Ms(minutesFromNow)
    };

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
        public List<BybitExecutionData> Fills { get; } = [];
        public List<BybitOrderData> OpenOrders { get; } = [];
        public Dictionary<string, BybitOrderData> OrdersById { get; } = [];
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
                .ReturnsAsync((string _, string _, BybitRegion _, string orderId, CancellationToken _) =>
                    harness.Fills.Where(fill => fill.OrderId == orderId).ToList());
            bybit.Setup(s => s.GetOpenOrdersAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => harness.OpenOrders.ToList());
            bybit.Setup(s => s.GetOrderAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string _, BybitRegion _, string orderId, CancellationToken _) => harness.OrdersById.GetValueOrDefault(orderId));
            bybit.Setup(s => s.GetExecutionsAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => harness.Fills.ToList());
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
