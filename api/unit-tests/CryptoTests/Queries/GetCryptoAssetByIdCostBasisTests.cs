using api.Cache;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Dtos;
using api.Cryptos.Queries;
using api.Cryptos.Repositories;
using api.Models.Cryptos;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace unit_tests.CryptoTests.Queries;

public class GetCryptoAssetByIdCostBasisTests
{
    private const decimal CurrentPrice = 80_000m;

    [Fact]
    public async Task Proportional_ValuesBalanceWithoutHistoryAtCurrentPriceAndShowsIt()
    {
        var cards = await CardsFor(TransferCostPolicy.Proportional);

        // 1 BTC bought at 60k; Bybit holds 1.5 BTC, so 0.5 BTC has no history and is valued at 80k.
        Value(cards, CryptoAssetData.WithoutHistoryTitle).Should().Be(0.5m);
        Value(cards, "Invested amount").Should().Be(100_000m);
        Value(cards, "Average price").Should().BeApproximately(66_666.67m, 0.01m);
        Value(cards, "Gain/Loss").Should().Be(20_000m);
    }

    [Fact]
    public async Task Legacy_KeepsZeroCostForBalanceWithoutHistory()
    {
        var cards = await CardsFor(TransferCostPolicy.Legacy);

        cards.Should().NotContain(card => card.Title == CryptoAssetData.WithoutHistoryTitle);
        Value(cards, "Invested amount").Should().Be(60_000m);
        Value(cards, "Average price").Should().Be(40_000m);
        Value(cards, "Gain/Loss").Should().Be(60_000m);
    }

    private static decimal Value(IEnumerable<CryptoAssetData> cards, string title) =>
        cards.Single(card => card.Title == title).Value;

    private static async Task<List<CryptoAssetData>> CardsFor(TransferCostPolicy policy)
    {
        var asset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        asset.AddHistoricalTransaction(new CryptoTransaction(1m, 60_000m, DateTimeOffset.UtcNow.AddDays(-1), "Bybit", ETransactionType.Buy, 0m));
        asset.ReconcileBalance(1.5m);

        var repository = new Mock<ICryptoAssetRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>>?>()))
            .ReturnsAsync(asset);
        var coinMarketCap = new Mock<ICoinMarketCapService>();
        coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>())).ReturnsAsync(new GetQuoteResponse(null!, new Dictionary<string, Coin>()));
        coinMarketCap.Setup(c => c.GetCryptoCurrencyPriceById(It.IsAny<int>(), It.IsAny<GetQuoteResponse>())).Returns(CurrentPrice);

        var handler = new GetCryptoAssetByIdQueryHandler(
            coinMarketCap.Object,
            repository.Object,
            NullLogger<GetCryptoAssetByIdQueryHandler>.Instance,
            new PassThroughCache(),
            Options.Create(new CostBasisSettings { TransferCostPolicy = policy }));

        var response = await handler.Handle(new GetCryptoAssetByIdQuery(CryptoAssetId: 1), CancellationToken.None);

        response.IsSuccess.Should().BeTrue(response.Message);
        return ((ViewCryptoAssetDto)response.Data!).CryptoAssetData;
    }

    private sealed class PassThroughCache : ICacheService
    {
        public Task<T> GetOrCreateAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
            => factory(cancellationToken);

        public void Remove(string cacheKey) { }

        public bool TryGetValue<T>(string cacheKey, out T value)
        {
            value = default!;
            return false;
        }
    }
}
