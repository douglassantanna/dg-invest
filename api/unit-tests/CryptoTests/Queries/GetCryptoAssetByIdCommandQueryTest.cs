using api.CoinMarketCap.Service;
using api.Cryptos.Queries;
using api.Data.Repositories;
using api.Models.Cryptos;
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
    public void GetCryptoAssetById_WhenBalanceIsZero_ShouldReturnPercentPriceDifferenceZero()
    {
        // Arrange
        var command = new GetCryptoAssetByIdCommandQuery(CryptoAssetId: 1);
        var handler = new GetCryptoAssetByIdCommandQueryHandler(_cryptoAssetRepositoryMock.Object, _coinMarketCapServiceMock.Object);
        // Act


        // Assert
    }
}
