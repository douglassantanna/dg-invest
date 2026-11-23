using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Queries;
using api.Cryptos.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.CryptoTests.Queries;

public class GetCryptoAssetByIdQueryTest
{
    [Fact]
    public async Task WhenNoPriceIsAvailableBecauseCoinMarketCapFailed_ReturnsAnError()
    {
        var handler = Handler(new PriceLookup(new Dictionary<int, decimal>(), FetchFailed: true));

        var response = await handler.Handle(new GetCryptoAssetByIdQuery(CryptoAssetId: 1), CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task WhenCoinMarketCapFailedButAPriceIsKnown_UsesIt()
    {
        var handler = Handler(new PriceLookup(new Dictionary<int, decimal> { [1] = 30m }, FetchFailed: true));

        var response = await handler.Handle(new GetCryptoAssetByIdQuery(CryptoAssetId: 1), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        ((ViewCryptoAssetDto)response.Data!).CryptoAssetData.Single(card => card.Title == "Current price").Value.Should().Be(30m);
    }

    [Fact]
    public async Task WhenCoinMarketCapDoesNotListTheCoin_StillReturnsTheAsset()
    {
        var handler = Handler(new PriceLookup(new Dictionary<int, decimal>(), FetchFailed: false));

        var response = await handler.Handle(new GetCryptoAssetByIdQuery(CryptoAssetId: 1), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        ((ViewCryptoAssetDto)response.Data!).CryptoAssetData.Single(card => card.Title == "Current price").Value.Should().Be(0m);
    }

    private static GetCryptoAssetByIdQueryHandler Handler(PriceLookup prices)
    {
        var asset = new CryptoAsset("BTC", "USD", "BTC", 1);
        asset.AddTransaction(new CryptoTransaction(1, 15, DateTimeOffset.Parse("2023-10-10"), "Binance", ETransactionType.Buy, 0));
        var repository = new Mock<ICryptoAssetRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>>?>()))
            .ReturnsAsync(asset);
        var priceService = new Mock<IPriceService>();
        priceService.Setup(p => p.GetPricesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(prices);
        return new GetCryptoAssetByIdQueryHandler(priceService.Object, repository.Object, NullLogger<GetCryptoAssetByIdQueryHandler>.Instance);
    }
}
