using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.CoinMarketCapTests;

public class PriceServiceTests
{
    private const int Btc = 1, Eth = 1027, Xtz = 2011;
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly Mock<ICoinMarketCapService> _coinMarketCap = new();

    private PriceService Service() => new(_coinMarketCap.Object, _cache, _clock, NullLogger<PriceService>.Instance);

    [Fact]
    public async Task FetchesAllRequestedCoinsInOneCall()
    {
        Quotes(new() { [Btc] = 60_000m, [Eth] = 3_000m });

        var lookup = await Service().GetPricesAsync([Btc, Eth, Btc]);

        lookup.PriceOf(Btc).Should().Be(60_000m);
        lookup.PriceOf(Eth).Should().Be(3_000m);
        lookup.FetchFailed.Should().BeFalse();
        _coinMarketCap.Verify(c => c.GetQuotesByIds(It.Is<string[]>(ids => ids.OrderBy(id => id).SequenceEqual(new[] { "1", "1027" }))), Times.Once);
    }

    [Fact]
    public async Task ReusesFreshPricesAcrossCallsAndOnlyFetchesMissingCoins()
    {
        Quotes(new() { [Btc] = 60_000m, [Eth] = 3_000m, [Xtz] = 0.21m });
        await Service().GetPricesAsync([Btc]);

        _clock.Advance(TimeSpan.FromSeconds(30));
        var lookup = await Service().GetPricesAsync([Btc, Xtz]);

        lookup.PriceOf(Btc).Should().Be(60_000m);
        lookup.PriceOf(Xtz).Should().Be(0.21m);
        _coinMarketCap.Verify(c => c.GetQuotesByIds(It.Is<string[]>(ids => ids.SequenceEqual(new[] { "2011" }))), Times.Once);
        _coinMarketCap.Verify(c => c.GetQuotesByIds(It.IsAny<string[]>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RefetchesAPriceOnceItIsOlderThanAMinute()
    {
        Quotes(new() { [Btc] = 60_000m });
        await Service().GetPricesAsync([Btc]);
        Quotes(new() { [Btc] = 61_000m });

        _clock.Advance(PriceService.FreshFor + TimeSpan.FromSeconds(1));
        var lookup = await Service().GetPricesAsync([Btc]);

        lookup.PriceOf(Btc).Should().Be(61_000m);
    }

    [Fact]
    public async Task WhenCoinMarketCapFails_UsesTheLastKnownPrice()
    {
        Quotes(new() { [Btc] = 60_000m });
        await Service().GetPricesAsync([Btc]);
        _coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>())).ThrowsAsync(new FlurlHttpException(null!));

        _clock.Advance(TimeSpan.FromMinutes(10));
        var lookup = await Service().GetPricesAsync([Btc, Eth]);

        lookup.PriceOf(Btc).Should().Be(60_000m);
        lookup.Prices.Should().NotContainKey(Eth);
        lookup.FetchFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ACoinCoinMarketCapDoesNotListIsAbsentWithoutAFailure()
    {
        Quotes(new() { [Btc] = 60_000m });

        var lookup = await Service().GetPricesAsync([Btc, 999_999]);

        lookup.Prices.Should().NotContainKey(999_999);
        lookup.FetchFailed.Should().BeFalse();
    }

    [Fact]
    public async Task ASlowerOlderRefreshDoesNotOverwriteANewerPrice()
    {
        // Request A starts first but CoinMarketCap answers it last; request B starts later and finishes first.
        var slowResponse = new TaskCompletionSource<GetQuoteResponse>();
        _coinMarketCap.SetupSequence(c => c.GetQuotesByIds(It.IsAny<string[]>()))
            .Returns(slowResponse.Task)
            .ReturnsAsync(Response(new() { [Btc] = 61_000m }));

        var requestA = Service().GetPricesAsync([Btc]);
        _clock.Advance(TimeSpan.FromSeconds(5));
        (await Service().GetPricesAsync([Btc])).PriceOf(Btc).Should().Be(61_000m);
        slowResponse.SetResult(Response(new() { [Btc] = 60_000m }));
        (await requestA).PriceOf(Btc).Should().Be(60_000m);

        // The newer price stays cached and fresh.
        _coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>())).ThrowsAsync(new InvalidOperationException("must not be called"));
        (await Service().GetPricesAsync([Btc])).PriceOf(Btc).Should().Be(61_000m);
    }

    [Fact]
    public async Task NoCoins_NoCall()
    {
        var lookup = await Service().GetPricesAsync([]);

        lookup.Prices.Should().BeEmpty();
        _coinMarketCap.Verify(c => c.GetQuotesByIds(It.IsAny<string[]>()), Times.Never);
    }

    private static GetQuoteResponse Response(Dictionary<int, decimal> prices) =>
        new(new Status(0, null), prices.ToDictionary(
            price => price.Key.ToString(),
            price => new Coin(price.Key, $"Coin {price.Key}", $"C{price.Key}", DateTime.UtcNow, new Quote(new USD(price.Value, DateTime.UtcNow, 0)))));

    private void Quotes(Dictionary<int, decimal> prices)
    {
        var data = prices.ToDictionary(
            price => price.Key.ToString(),
            price => new Coin(price.Key, $"Coin {price.Key}", $"C{price.Key}", DateTime.UtcNow, new Quote(new USD(price.Value, DateTime.UtcNow, 0))));
        _coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>()))
            .ReturnsAsync((string[] ids) => new GetQuoteResponse(new Status(0, null),
                data.Where(entry => ids.Contains(entry.Key)).ToDictionary(entry => entry.Key, entry => entry.Value)));
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
