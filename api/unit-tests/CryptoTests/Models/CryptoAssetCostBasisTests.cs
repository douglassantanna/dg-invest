using api.Cryptos.Models;
using api.Models.Cryptos;
using FluentAssertions;

namespace unit_tests.CryptoTests.Models;

// Cost basis uses the average-cost method. TransferCostPolicy.Proportional fixes transfers; Legacy keeps the
// behaviour shipped before the fix so the rollout can be compared before it is switched on.
public class CryptoAssetCostBasisTests
{
    private static readonly DateTimeOffset Day = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static CryptoTransaction Tx(ETransactionType type, decimal amount, decimal price, int day, decimal fee = 0) =>
        new(amount, price, Day.AddDays(day), "Test", type, fee);

    private static CryptoAsset Asset(params CryptoTransaction[] transactions)
    {
        var asset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        foreach (var transaction in transactions)
            asset.AddHistoricalTransaction(transaction);
        return asset;
    }

    [Fact]
    public void Proportional_TransferOutRemovesCostAtTheAveragePrice()
    {
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0), Tx(ETransactionType.TransferOut, 0.5m, 80_000m, 1));

        asset.RecalculateFromTransactions(TransferCostPolicy.Proportional);

        asset.Balance.Should().Be(0.5m);
        asset.TotalInvested.Should().Be(30_000m);
        asset.AveragePrice.Should().Be(60_000m);
    }

    [Fact]
    public void Legacy_TransferOutKeepsTheFullCost()
    {
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0), Tx(ETransactionType.TransferOut, 0.5m, 80_000m, 1));

        asset.RecalculateFromTransactions(TransferCostPolicy.Legacy);

        asset.TotalInvested.Should().Be(60_000m);
        asset.AveragePrice.Should().Be(120_000m);
    }

    [Fact]
    public void Proportional_TransferInAddsCostAtTheRecordedPrice()
    {
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0), Tx(ETransactionType.TransferIn, 1m, 80_000m, 1));

        asset.RecalculateFromTransactions(TransferCostPolicy.Proportional);

        asset.Balance.Should().Be(2m);
        asset.TotalInvested.Should().Be(140_000m);
        asset.AveragePrice.Should().Be(70_000m);
    }

    [Fact]
    public void Legacy_TransferInAddsNoCost()
    {
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0), Tx(ETransactionType.TransferIn, 1m, 80_000m, 1));

        asset.RecalculateFromTransactions(TransferCostPolicy.Legacy);

        asset.TotalInvested.Should().Be(60_000m);
    }

    [Fact]
    public void Proportional_ManualTransfersFollowTheSameRules()
    {
        var asset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);

        asset.AddTransaction(Tx(ETransactionType.Buy, 1m, 60_000m, 0), TransferCostPolicy.Proportional);
        asset.AddTransaction(Tx(ETransactionType.TransferIn, 1m, 80_000m, 1), TransferCostPolicy.Proportional);
        asset.AddTransaction(Tx(ETransactionType.TransferOut, 1m, 0m, 2), TransferCostPolicy.Proportional);

        asset.Balance.Should().Be(1m);
        asset.TotalInvested.Should().Be(70_000m);
        asset.AveragePrice.Should().Be(70_000m);
    }

    [Fact]
    public void CostBasis_ValuesBalanceWithoutHistoryAtTheCurrentPrice()
    {
        // Account 33 (STAGE, 2026-11-09): 8 BTC buys totalling 0.000459 BTC, while the Bybit wallet holds 0.00052366.
        var asset = Asset(
            Tx(ETransactionType.Buy, 0.000025m, 77_122m, 0, fee: 0.00000003m * 77_122m),
            Tx(ETransactionType.Buy, 0.000064m, 77_142.8m, 0, fee: 0.00000006m * 77_142.8m),
            Tx(ETransactionType.Buy, 0.000129m, 76_974m, 4, fee: 0.00000013m * 76_974m),
            Tx(ETransactionType.Buy, 0.000052m, 76_657.9m, 4, fee: 0.00000005m * 76_657.9m),
            Tx(ETransactionType.Buy, 0.000039m, 76_645.1m, 4, fee: 0.00000004m * 76_645.1m),
            Tx(ETransactionType.Buy, 0.000061m, 81_133m, 7, fee: 0.00000006m * 81_133m),
            Tx(ETransactionType.Buy, 0.000049m, 80_708.6m, 52, fee: 0.00000005m * 80_708.6m),
            Tx(ETransactionType.Buy, 0.000040m, 74_661.5m, 56, fee: 0.00000004m * 74_661.5m));
        asset.ReconcileBalance(0.00052366m);
        const decimal currentPrice = 74_710.65860909219m;

        var legacy = asset.GetCostBasis(currentPrice, TransferCostPolicy.Legacy);
        var proportional = asset.GetCostBasis(currentPrice, TransferCostPolicy.Proportional);

        // Legacy: the 0.00006466 BTC without history counts as free, so the average drops to ~68.2k and the gain looks positive.
        legacy.UnexplainedBalance.Should().Be(0m);
        legacy.AveragePrice.Should().BeApproximately(68_166.8m, 1m);
        legacy.GainLossPercentage.Should().BeGreaterThan(9m);

        // Proportional: that part is valued at the current price, so it adds no gain and no loss.
        proportional.UnexplainedBalance.Should().Be(0.00006466m);
        proportional.Invested.Should().BeApproximately(35.69623672m + 0.00006466m * currentPrice, 0.0001m);
        proportional.AveragePrice.Should().BeApproximately(77_391m, 5m);
        proportional.GainLoss.Should().BeApproximately(0.00052366m * currentPrice - proportional.Invested, 0.0001m);
        proportional.GainLossPercentage.Should().BeApproximately(-3.47m, 0.05m);
    }

    [Fact]
    public void Preview_ShowsTheProportionalResultWithoutChangingTheAsset()
    {
        // Stored values were computed with Legacy: the withdrawal kept the full cost.
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0), Tx(ETransactionType.TransferOut, 0.5m, 80_000m, 1));
        asset.TotalInvested.Should().Be(60_000m);

        var preview = asset.PreviewCostBasis(80_000m, TransferCostPolicy.Proportional);

        preview.Invested.Should().Be(30_000m);
        preview.AveragePrice.Should().Be(60_000m);
        preview.GainLoss.Should().Be(10_000m);
        asset.TotalInvested.Should().Be(60_000m, "a preview must not change the stored values");
    }

    [Fact]
    public void Preview_ReconcilesToTheCurrentBalanceLikeTheNextSync()
    {
        // History explains 1 BTC at 60k, but Bybit reported only 0.8 BTC (e.g. fees in BTC): cost scales down.
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0));
        asset.ReconcileBalance(0.8m);

        var preview = asset.PreviewCostBasis(80_000m, TransferCostPolicy.Proportional);

        preview.Invested.Should().Be(48_000m);
        preview.AveragePrice.Should().Be(60_000m);
        preview.UnexplainedBalance.Should().Be(0m);
    }

    [Fact]
    public void CostBasis_WithoutGapMatchesTheStoredValues()
    {
        var asset = Asset(Tx(ETransactionType.Buy, 1m, 60_000m, 0));

        var costBasis = asset.GetCostBasis(80_000m, TransferCostPolicy.Proportional);

        costBasis.UnexplainedBalance.Should().Be(0m);
        costBasis.Invested.Should().Be(60_000m);
        costBasis.AveragePrice.Should().Be(60_000m);
        costBasis.GainLoss.Should().Be(20_000m);
    }
}
