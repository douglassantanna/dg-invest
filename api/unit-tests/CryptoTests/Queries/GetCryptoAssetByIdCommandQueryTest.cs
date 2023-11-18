using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data.Repositories;
using api.Models.Cryptos;
using FluentAssertions;
using Moq;

namespace unit_tests.CryptoTests.Queries;
public class GetCryptoAssetByIdCommandQueryTest
{
    private readonly Mock<IBaseRepository<CryptoAsset>> _cryptoAssetRepositoryMock;
    private readonly Mock<ICoinMarketCapService> _coinMarketCapServiceMock;

    public GetCryptoAssetByIdCommandQueryTest()
    {
        _cryptoAssetRepositoryMock = new Mock<IBaseRepository<CryptoAsset>>();
        _coinMarketCapServiceMock = new Mock<ICoinMarketCapService>();
    }

    [Fact]
    public async Task GetCryptoAssetById_WhenBalanceIsZero_ShouldReturnPercentPriceDifferenceZero()
    {
        // Arrange
        var command = new GetCryptoAssetByIdCommandQuery(CryptoAssetId: 1);
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), CancellationToken.None))
                                  .ReturnsAsync(GetCryptoAsset());

        _coinMarketCapServiceMock.Setup(x => x.GetQuotesByIds(new[] { It.IsAny<string>() }))
                             .ReturnsAsync(It.IsAny<GetQuoteResponse>());

        var handler = new GetCryptoAssetByIdCommandQueryHandler(_coinMarketCapServiceMock.Object, _cryptoAssetRepositoryMock.Object);
        // Act

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        ViewCryptoAssetDto? viewResult = result.Data as ViewCryptoAssetDto;
        viewResult?.CryptoInformation.PercentDifference.Should().Be(0);
    }

    private static CryptoAsset GetCryptoAsset()
    {
        CryptoAsset cryptoAsset = new("BTC", "USD", "BTC", 1);
        cryptoAsset.AddAddress(new Address());
        List<CryptoTransaction> transactions = new()
        {
            new(amount: 1,
                price: 15,
                purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Buy),
            new(amount: 1,
                price: 28,
                purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Buy),
            new(amount: 2,
                price: 50,
                purchaseDate: DateTimeOffset.Parse("2023-12-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Sell)
        };

        foreach (var transaction in transactions)
        {
            cryptoAsset.AddTransaction(transaction);
        }

        return cryptoAsset;
    }
}
