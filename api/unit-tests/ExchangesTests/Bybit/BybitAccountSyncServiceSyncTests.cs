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
        public List<BybitOrderData> Orders { get; } = [];
        public List<BybitDepositWithdrawalRow> Deposits { get; } = [];
        public Dictionary<(string AccountType, string Coin), string> CashBalances { get; } = [];
        public Exception? CashBalanceError { get; set; }
        public string? LastCheckpointOrderId { get; private set; }

        public static async Task<Harness> CreateAsync(decimal localBalance = 0)
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
            syncStatus.EnableForCredentials();
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

            var harness = new Harness { Context = context, Account = account, Service = service, OrderSync = orderSync };

            bybit.Setup(s => s.GetOrderHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
                .ReturnsAsync(() => harness.Orders.ToList());
            bybit.Setup(s => s.GetDepositHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                    It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
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
                .Callback<int, int, string?, CancellationToken, DateTime?>((_, _, orderId, _, _) => harness.LastCheckpointOrderId = orderId)
                .Returns(Task.CompletedTask);

            return harness;
        }
    }
}
