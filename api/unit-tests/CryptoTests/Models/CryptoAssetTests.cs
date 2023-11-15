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
        // Arrange
        var cryptoAsset = _validCryptoAsset;

        // Act
        cryptoAsset.AddAddress(new Address());

        // Assert
        cryptoAsset.Addresses.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(48, 14, 62)]
    public void CryptoAsset_WhenAddedBuyTransactions_TotalInvestedShouldBeEqualsToSumOfTransactionsPrice(decimal price1, decimal price2, decimal expectedTotalCost)
    {
        // Arrange
        var cryptoAsset = _validCryptoAsset;

        // Act
        AddTransactions(price1, price2, cryptoAsset);

        // Assert
        cryptoAsset.TotalInvested.Should().Be(expectedTotalCost);
    }
    [Fact]
    public void CryptoAsset_WhenAddedSellTransactionsWithZeroTotalAmount_ShouldThrowException()
    {
        // Arrange
        var cryptoAsset = _validCryptoAsset;
        CryptoTransaction transaction = new(amount: 1,
                                            price: 10,
                                            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                                            exchangeName: "Binance",
                                            transactionType: ETransactionType.Sell);

        // Act
        var result = () => cryptoAsset.AddTransaction(transaction);

        // Assert
        result.Should().Throw<CryptoAssetException>();
    }
    [Fact]
    public void CryptoAsset_WhenAddedSellTransactions_AmountShouldBeDeductedFromBalance()
    {
        // Arrange
        var cryptoAsset = _validCryptoAsset;
        CryptoTransaction transaction = new(amount: 1,
                                            price: 10,
                                            purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                                            exchangeName: "Binance",
                                            transactionType: ETransactionType.Sell);

        // Act
        cryptoAsset.AddBalance(1); // need to add some balance first, so it can be deducted
        cryptoAsset.AddTransaction(transaction);


        // Assert
        cryptoAsset.Balance.Should().Be(0);
    }
    [Theory]
    [InlineData(10, 15, 15, 20)]
    [InlineData(2, 5, 35, 900)]
    public void GetPercentDifference_GivenCurrentPrice_ShouldReturnExpectedPercentDifference(
        decimal price1,
        decimal price2,
        decimal currentPrice,
        decimal expectedPercentDifference)
    {
        // Arrange
        var cryptoAsset = _validCryptoAsset;
        AddTransactions(price1, price2, cryptoAsset);

        // Act
        var result = cryptoAsset.GetPercentDifference(currentPrice);

        // Assert
        result.Should().Be(expectedPercentDifference);
    }

    private static void AddTransactions(decimal price1,
                                        decimal price2,
                                        CryptoAsset cryptoAsset)
    {
        List<CryptoTransaction> transactions = new()
        {
            new(amount: 1,
                price: price1,
                purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Buy),
            new(amount: 1,
                price: price2,
                purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Buy)
        };

        foreach (var transaction in transactions)
        {
            cryptoAsset.AddTransaction(transaction);
        }
    }
}
