using api.AzureStorage;
using api.AzureStorage.Blob;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Services;

// Crypto moved between the user's own Bybit UIDs keeps its cost, and deposits are priced at the time they happened.
public class BybitTransferCostCarryTests
{
    private const string MasterUid = "100";
    private const string SubUid = "200";
    private static readonly DateTimeOffset BoughtAt = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TransferredAt = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CryptoTransferBetweenOwnAccounts_RecordsBothSidesAndCarriesTheSourceAveragePrice()
    {
        using var context = CreateContext();
        var master = await AddAccountAsync(context, MasterUid, ("BTC", 1m, 50_000m));
        var sub = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var bybit = BybitWithCandlePrice(90_000m);
        var sut = CreateService(context, bybit.Object);
        var transfer = BtcTransfer("t-1", "0.4", from: MasterUid, to: SubUid);

        // The destination syncs first: the source's TransferOut is not recorded yet, and must not matter.
        (await sut.ProcessInternalTransferAsync(transfer, sub, 1, CancellationToken.None, applyCashBalance: false)).Should().BeTrue();
        (await sut.ProcessInternalTransferAsync(transfer, master, 1, CancellationToken.None, applyCashBalance: false)).Should().BeTrue();

        var received = Btc(sub).Transactions.Single();
        received.TransactionType.Should().Be(ETransactionType.TransferIn);
        received.Amount.Should().Be(0.4m);
        received.Price.Should().Be(50_000m);
        Btc(sub).TotalInvested.Should().Be(20_000m);
        Btc(master).Transactions.Should().ContainSingle(t => t.TransactionType == ETransactionType.TransferOut && t.Amount == 0.4m);
        Btc(master).Balance.Should().Be(0.6m);
        Btc(master).TotalInvested.Should().Be(30_000m);

        // History only: cash and total deposited stay untouched on both sides.
        sub.AccountTransactions.Single().TransactionType.Should().Be(EAccountTransactionType.DepositCrypto);
        master.AccountTransactions.Single().TransactionType.Should().Be(EAccountTransactionType.WithdrawCrypto);
        sub.Balance.Should().Be(0m);
        master.Balance.Should().Be(0m);
        sub.TotalDeposited().Should().Be(0m);
        master.TotalDeposited().Should().Be(0m);
        bybit.Verify(b => b.GetSpotPriceAtAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CryptoTransfer_WhenTheSourceHistoryDoesNotExplainTheQuantity_UsesTheMarketPriceAtTheTransfer()
    {
        using var context = CreateContext();
        await AddAccountAsync(context, MasterUid, ("BTC", 0.1m, 50_000m));
        var sub = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var bybit = BybitWithCandlePrice(60_000m);
        var sut = CreateService(context, bybit.Object);

        await sut.ProcessInternalTransferAsync(BtcTransfer("t-1", "0.4", from: MasterUid, to: SubUid), sub, 1, CancellationToken.None, applyCashBalance: false);

        Btc(sub).Transactions.Single().Price.Should().Be(60_000m);
        bybit.Verify(b => b.GetSpotPriceAtAsync("BTC", TransferredAt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CryptoTransfer_WhenTheSourceUidIsNotAnAppAccount_UsesTheMarketPriceAtTheTransfer()
    {
        using var context = CreateContext();
        var sub = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var sut = CreateService(context, BybitWithCandlePrice(60_000m).Object);

        await sut.ProcessInternalTransferAsync(BtcTransfer("t-1", "0.4", from: "999", to: SubUid), sub, 1, CancellationToken.None, applyCashBalance: false);

        Btc(sub).Transactions.Single().Price.Should().Be(60_000m);
    }

    [Fact]
    public async Task CryptoTransfer_WhenProcessedTwice_IsRecordedOnce()
    {
        using var context = CreateContext();
        var sub = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var sut = CreateService(context, BybitWithCandlePrice(60_000m).Object);
        var transfer = BtcTransfer("t-1", "0.4", from: MasterUid, to: SubUid);

        await sut.ProcessInternalTransferAsync(transfer, sub, 1, CancellationToken.None, applyCashBalance: false);
        await sut.ProcessInternalTransferAsync(transfer, sub, 1, CancellationToken.None, applyCashBalance: false);

        Btc(sub).Transactions.Should().ContainSingle();
        (await context.AccountTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CryptoDeposit_IsPricedAtTheDepositTime()
    {
        using var context = CreateContext();
        var account = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var bybit = BybitWithCandlePrice(61_000m);
        var sut = CreateService(context, bybit.Object, CoinMarketCapWithPrice(99_000m).Object);

        await sut.ProcessDepositAsync(BtcDeposit(), account, 1, CancellationToken.None, applyCashBalance: false);

        Btc(account).Transactions.Single().Price.Should().Be(61_000m);
        bybit.Verify(b => b.GetSpotPriceAtAsync("BTC", TransferredAt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CryptoDeposit_WhenBybitHasNoCandle_FallsBackToTheCurrentCoinMarketCapPrice()
    {
        using var context = CreateContext();
        var account = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var sut = CreateService(context, BybitWithCandlePrice(null).Object, CoinMarketCapWithPrice(99_000m).Object);

        await sut.ProcessDepositAsync(BtcDeposit(), account, 1, CancellationToken.None, applyCashBalance: false);

        Btc(account).Transactions.Single().Price.Should().Be(99_000m);
    }

    [Fact]
    public async Task CryptoDeposit_WhenTheCandleRequestFails_ThrowsSoTheSyncRetries()
    {
        // A temporary failure must not record today's price for good.
        using var context = CreateContext();
        var account = await AddAccountAsync(context, SubUid, ("BTC", 0m, 0m));
        var bybit = new Mock<IBybitService>();
        bybit.Setup(b => b.GetSpotPriceAtAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BybitTransportException("/v5/market/kline", new HttpRequestException("timeout")));
        var sut = CreateService(context, bybit.Object, CoinMarketCapWithPrice(99_000m).Object);

        var act = () => sut.ProcessDepositAsync(BtcDeposit(), account, 1, CancellationToken.None, applyCashBalance: false);

        await act.Should().ThrowAsync<BybitTransportException>();
        Btc(account).Transactions.Should().BeEmpty();
    }

    private static CryptoAsset Btc(Account account) => account.CryptoAssets.Single(a => a.Symbol == "BTC");

    private static BybitInternalTransferRow BtcTransfer(string id, string amount, string from, string to) => new()
    {
        TransferId = id,
        Coin = "BTC",
        Amount = amount,
        FromAccountType = "UNIFIED",
        ToAccountType = "UNIFIED",
        FromMemberId = from,
        ToMemberId = to,
        Timestamp = TransferredAt.ToUnixTimeMilliseconds().ToString()
    };

    private static BybitDepositWithdrawalRow BtcDeposit() => new()
    {
        Coin = "BTC",
        Amount = "0.5",
        Status = "3",
        TxId = "btc-deposit-1",
        SuccessAt = TransferredAt.ToUnixTimeMilliseconds().ToString()
    };

    // An exchange account holding the coin; a non-zero quantity is bought at BoughtAt for the given price.
    private static async Task<Account> AddAccountAsync(DataContext context, string uid, (string Symbol, decimal Quantity, decimal Price) holding)
    {
        var account = new Account($"Bybit {uid}", 1, EAccountType.Exchange, "Bybit", uid);
        var asset = new CryptoAsset("Bitcoin", "Bitcoin", holding.Symbol, 1);
        account.AddCryptoAsset(asset);
        if (holding.Quantity > 0)
            asset.AddTransaction(new CryptoTransaction(holding.Quantity, holding.Price, BoughtAt, "Bybit", ETransactionType.Buy, 0));
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    private static Mock<IBybitService> BybitWithCandlePrice(decimal? price)
    {
        var bybit = new Mock<IBybitService>();
        bybit.Setup(b => b.GetSpotPriceAtAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);
        return bybit;
    }

    private static Mock<ICoinMarketCapService> CoinMarketCapWithPrice(decimal price)
    {
        var coinMarketCap = new Mock<ICoinMarketCapService>();
        coinMarketCap.Setup(c => c.GetQuoteBySymbol(It.IsAny<string>()))
            .ReturnsAsync(new GetQuoteResponse(new Status(0, null), new Dictionary<string, Coin>
            {
                ["BTC"] = new(1, "Bitcoin", "BTC", DateTime.UtcNow, new Quote(new USD(price, DateTime.UtcNow, 0)))
            }));
        return coinMarketCap;
    }

    private static DataContext CreateContext() =>
        new(new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static BybitOrderSyncService CreateService(DataContext context, IBybitService bybit, ICoinMarketCapService? coinMarketCap = null)
    {
        var blobStorage = new Mock<IBlobStorageService>();
        blobStorage.Setup(x => x.AppendLogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new BybitOrderSyncService(
            coinMarketCap ?? Mock.Of<ICoinMarketCapService>(),
            new TransactionService([]),
            blobStorage.Object,
            Options.Create(new AzureStorageSettings()),
            context,
            Mock.Of<ILogger<BybitOrderSyncService>>(),
            Mock.Of<ICacheService>(),
            Options.Create(new CostBasisSettings { TransferCostPolicy = TransferCostPolicy.Proportional }),
            bybit);
    }
}
