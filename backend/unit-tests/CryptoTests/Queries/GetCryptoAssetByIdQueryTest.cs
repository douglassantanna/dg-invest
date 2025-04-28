using api.Cache;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Cryptos.Repositories;
using api.Models.Cryptos;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.CryptoTests.Queries;
public class GetCryptoAssetByIdQueryTest
{
    private readonly Mock<ICryptoAssetRepository> _cryptoAssetRepositoryMock;
    private readonly Mock<ICoinMarketCapService> _coinMarketCapServiceMock;
    private readonly Mock<ILogger<GetCryptoAssetByIdQueryHandler>> _loggerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public GetCryptoAssetByIdQueryTest()
    {
        _cryptoAssetRepositoryMock = new Mock<ICryptoAssetRepository>();
        _coinMarketCapServiceMock = new Mock<ICoinMarketCapService>();
        _loggerMock = new Mock<ILogger<GetCryptoAssetByIdQueryHandler>>();
        _cacheServiceMock = new Mock<ICacheService>();
    }

    [Fact]
    public async Task GetCryptoAssetById_WhenBalanceIsZero_ShouldReturnPercentPriceDifferenceZero()
    {
        // Arrange
        var command = new GetCryptoAssetByIdQuery(CryptoAssetId: 1);
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), null))
                                  .ReturnsAsync(GetCryptoAsset());

        _coinMarketCapServiceMock.Setup(x => x.GetQuotesByIds(new[] { It.IsAny<string>() }))
                             .ReturnsAsync(It.IsAny<GetQuoteResponse>());

        var handler = new GetCryptoAssetByIdQueryHandler(_coinMarketCapServiceMock.Object,
                                                         _cryptoAssetRepositoryMock.Object,
                                                         _loggerMock.Object,
                                                         _cacheServiceMock.Object);
        // Act

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        ViewCryptoAssetDto? viewResult = result.Data as ViewCryptoAssetDto;
        // viewResult?.Cards[0].Percent.Should().Be(0);
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
                transactionType: ETransactionType.Buy,
                fee: 0.0m),
            new(amount: 1,
                price: 28,
                purchaseDate: DateTimeOffset.Parse("2023-10-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Buy,
                fee: 0.0m),
            new(amount: 2,
                price: 50,
                purchaseDate: DateTimeOffset.Parse("2023-12-10"),
                exchangeName: "Binance",
                transactionType: ETransactionType.Sell,
                fee: 0.0m)
        };

        foreach (var transaction in transactions)
        {
            cryptoAsset.AddTransaction(transaction);
        }

        return cryptoAsset;
    }
}
