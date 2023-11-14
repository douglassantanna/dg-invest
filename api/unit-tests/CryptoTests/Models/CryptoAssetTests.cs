using System.Transactions;
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
        List<CryptoTransaction> transactions = new List<CryptoTransaction>
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

        // Assert
        cryptoAsset.TotalInvested.Should().Be(expectedTotalCost);
    }
}
