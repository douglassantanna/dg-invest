using api.Cryptos.Exceptions;
using api.Cryptos.Models;
using api.Models.Cryptos;
using FluentAssertions;

namespace unit_tests.CryptoTests.Models;
public class CryptoAssetTests
{
    private readonly CryptoAsset _validCryptoAsset;

    public CryptoAssetTests()
    {
        _validCryptoAsset = new CryptoAsset("BTC", "USD", "BTC", 1);
    }

    [Fact]
    public void CreateCryptoAsset_WhenAddedAddress_AddressListShouldNotBeEmpty()
    {
        var cryptoAsset = _validCryptoAsset;
        cryptoAsset.AddAddress(new Address());
        cryptoAsset.Addresses.Should().HaveCount(1);
    }

    // BUY: TotalInvested = (amount * price) + fee
    // Two buys of amount=1: price1 with fee=0 and price2 with fee=0
    // TotalInvested = price1 + price2
    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(48, 14, 62)]
    public void CryptoAsset_WhenAddedBuyTransactions_TotalInvestedShouldBeCorrectCostBasis(
        decimal price1, decimal price2, decimal expectedTotalCost)
    {
        var cryptoAsset = _validCryptoAsset;
        AddBuyTransactions(price1, price2, cryptoAsset);
        cryptoAsset.TotalInvested.Should().Be(expectedTotalCost);
    }

    [Fact]
    public void CryptoAsset_WhenBuyIncludesFee_TotalInvestedShouldIncludeFee()
    {
        var cryptoAsset = _validCryptoAsset;
        var transaction = new CryptoTransaction(
            amount: 1,
            price: 100,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 5);

        cryptoAsset.AddTransaction(transaction);

        cryptoAsset.TotalInvested.Should().Be(105);
        cryptoAsset.AveragePrice.Should().Be(105);
    }

    [Fact]
    public void AveragePrice_ShouldBeWeightedAverage_NotSimpleAverage()
    {
        // Buy 1 BTC at 10 000 and 2 BTC at 20 000 (no fees)
        // TotalInvested = 10 000 + 40 000 = 50 000
        // Balance = 3
        // AveragePrice = 50 000 / 3 ≈ 16 666.67
        var cryptoAsset = _validCryptoAsset;

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: 10_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 2, price: 20_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-11"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AveragePrice.Should().BeApproximately(50_000m / 3m, 0.01m);
    }

    [Fact]
    public void CryptoAsset_WhenSellTransactionWithZeroBalance_ShouldThrowException()
    {
        var cryptoAsset = _validCryptoAsset;
        var sellTx = new CryptoTransaction(
            amount: 1, price: 10,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Sell,
            fee: 0);

        var act = () => cryptoAsset.AddTransaction(sellTx);

        act.Should().Throw<CryptoAssetException>();
    }

    [Fact]
    public void CryptoAsset_WhenSellTransaction_BalanceShouldDecrement()
    {
        var cryptoAsset = _validCryptoAsset;
        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: 10,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: 12,
            purchaseDate: DateTimeOffset.Parse("2023-10-11"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Sell,
            fee: 0));

        cryptoAsset.Balance.Should().Be(0);
    }

    [Fact]
    public void CryptoAsset_WhenSellReducesBalanceToZero_TotalInvestedShouldBeZero()
    {
        var cryptoAsset = _validCryptoAsset;
        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 2, price: 50_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 2, price: 60_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-11"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Sell,
            fee: 0));

        cryptoAsset.Balance.Should().Be(0);
        cryptoAsset.TotalInvested.Should().Be(0);
        cryptoAsset.AveragePrice.Should().Be(0);
    }

    [Fact]
    public void CryptoAsset_WhenPartialSell_TotalInvestedShouldReflectRemainingCostBasis()
    {
        // Buy 2 BTC at 50 000 → TotalInvested = 100 000, Balance = 2, AvgPrice = 50 000
        // Sell 1 BTC → CostBasisRemoved = 1 * 50 000 = 50 000
        // TotalInvested = 50 000, Balance = 1, AvgPrice = 50 000 (unchanged)
        var cryptoAsset = _validCryptoAsset;
        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 2, price: 50_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: 60_000,
            purchaseDate: DateTimeOffset.Parse("2023-10-11"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Sell,
            fee: 0));

        cryptoAsset.Balance.Should().Be(1);
        cryptoAsset.TotalInvested.Should().Be(50_000);
        cryptoAsset.AveragePrice.Should().Be(50_000);
    }

    [Fact]
    public void CryptoAsset_TotalInvested_ShouldNeverBeNegativeAfterSell()
    {
        // Sell price higher than avg should not produce negative TotalInvested
        var cryptoAsset = _validCryptoAsset;
        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: 100,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 0.5m, price: 999_999,
            purchaseDate: DateTimeOffset.Parse("2023-10-11"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Sell,
            fee: 0));

        cryptoAsset.TotalInvested.Should().BeGreaterThanOrEqualTo(0);
    }

    // AveragePrice = TotalInvested / Balance
    // Two buys of amount=1 at price1 and price2 (no fees) → AveragePrice = (price1 + price2) / 2
    [Theory]
    [InlineData(10, 15, 15, 20)]  // avgPrice=12.5, currentPrice=15 → (15-12.5)/12.5*100=20
    [InlineData(2, 5, 35, 900)]   // avgPrice=3.5, currentPrice=35 → (35-3.5)/3.5*100=900
    public void GetPercentDifference_GivenCurrentPrice_ShouldReturnExpectedPercentDifference(
        decimal price1,
        decimal price2,
        decimal currentPrice,
        decimal expectedPercentDifference)
    {
        var cryptoAsset = _validCryptoAsset;
        AddBuyTransactions(price1, price2, cryptoAsset);

        var result = cryptoAsset.GetPercentDifference(currentPrice);

        result.Should().Be(expectedPercentDifference);
    }

    private static void AddBuyTransactions(decimal price1, decimal price2, CryptoAsset cryptoAsset)
    {
        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: price1,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));

        cryptoAsset.AddTransaction(new CryptoTransaction(
            amount: 1, price: price2,
            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
            exchangeName: "Binance",
            transactionType: ETransactionType.Buy,
            fee: 0));
    }
}
