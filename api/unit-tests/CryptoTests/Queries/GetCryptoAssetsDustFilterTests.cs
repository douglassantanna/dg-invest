using api.Cache;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using Flurl.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.CryptoTests.Queries;

// "Hide zero and dust balances" on /cryptos hides assets worth less than US$1 (issue #329).
public class GetCryptoAssetsDustFilterTests
{
    private const int Btc = 1, Uma = 5617, Jup = 29210;

    [Fact]
    public async Task HideZeroBalance_HidesAssetsWorthLessThanOneDollarIncludingBalanceWithoutHistory()
    {
        await using var context = await AccountWithAssetsAsync();
        var handler = Handler(context, PricedCoinMarketCap(new() { [Btc] = 50_000m, [Uma] = 1.20m }));

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: true);

        // BTC worth $5; UMA (wallet dust without history) worth $0.0012; JUP has no price, so the old rule keeps it.
        visible.Should().BeEquivalentTo(["btc", "jup"]);
    }

    [Fact]
    public async Task HideZeroBalanceOff_ShowsEveryAsset()
    {
        await using var context = await AccountWithAssetsAsync();
        var handler = Handler(context, PricedCoinMarketCap(new() { [Btc] = 50_000m, [Uma] = 1.20m }));

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: false);

        visible.Should().BeEquivalentTo(["btc", "uma", "jup"]);
    }

    [Fact]
    public async Task HideZeroBalance_WhenCoinMarketCapFails_DoesNotEmptyTheList()
    {
        await using var context = await AccountWithAssetsAsync();
        var coinMarketCap = new Mock<ICoinMarketCapService>();
        coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>())).ThrowsAsync(new FlurlHttpException(null!));
        coinMarketCap.Setup(c => c.GetCryptoCurrencyPriceById(It.IsAny<int>(), It.IsAny<GetQuoteResponse>())).Returns(0m);
        var handler = Handler(context, coinMarketCap.Object);

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: true);

        visible.Should().Contain("btc");
    }

    [Fact]
    public async Task CachedList_AfterSwitchingAccount_ReturnsTheNewlySelectedAccount()
    {
        // The list is cached for a minute. The key must include the selected account: switching only clears the last
        // cached variant, so another variant (here: the other toggle value) would still hold the previous account.
        await using var context = await AccountWithAssetsAsync();
        var brother = new Account("brother", 1);
        context.Accounts.Add(brother);
        await context.SaveChangesAsync();
        var cache = new MemoryCacheService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var handler = new GetCryptoAssetsQueryHandler(context, PricedCoinMarketCap(new() { [Btc] = 50_000m, [Uma] = 1.20m }),
            NullLogger<GetCryptoAssetsQueryHandler>.Instance, cache);

        (await AccountTagAsync(handler, hideZeroBalance: true)).Should().Be("main");
        await context.Accounts.Where(account => account.Name == "main").ForEachAsync(account => account.Deselect());
        brother.Select();
        await context.SaveChangesAsync();

        (await AccountTagAsync(handler, hideZeroBalance: true)).Should().Be("brother");
    }

    private static async Task<string> AccountTagAsync(GetCryptoAssetsQueryHandler handler, bool hideZeroBalance)
    {
        var page = await handler.Handle(new GetCryptoAssetsQuery { UserId = 1, PageSize = 50, HideZeroBalance = hideZeroBalance }, CancellationToken.None);
        return page.Items.Single().AccountTag;
    }

    private static async Task<DataContext> AccountWithAssetsAsync()
    {
        var context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var account = new Account("main", 1);
        var btc = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", Btc);
        btc.AddTransaction(new CryptoTransaction(0.0001m, 40_000m, DateTimeOffset.UtcNow.AddDays(-1), "Bybit", ETransactionType.Buy, 0));
        var uma = new CryptoAsset("UMA", "UMA", "UMA", Uma);
        uma.AddBalance(0.001m);
        var jup = new CryptoAsset("Jupiter", "Jupiter", "JUP", Jup);
        jup.AddBalance(0.00966m);
        account.AddCryptoAsset(btc);
        account.AddCryptoAsset(uma);
        account.AddCryptoAsset(jup);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return context;
    }

    private static ICoinMarketCapService PricedCoinMarketCap(Dictionary<int, decimal> prices)
    {
        var coinMarketCap = new Mock<ICoinMarketCapService>();
        coinMarketCap.Setup(c => c.GetQuotesByIds(It.IsAny<string[]>())).ReturnsAsync(new GetQuoteResponse(null!, new Dictionary<string, Coin>()));
        coinMarketCap.Setup(c => c.GetCryptoCurrencyPriceById(It.IsAny<int>(), It.IsAny<GetQuoteResponse>()))
            .Returns((int id, GetQuoteResponse _) => prices.GetValueOrDefault(id));
        return coinMarketCap.Object;
    }

    private static GetCryptoAssetsQueryHandler Handler(DataContext context, ICoinMarketCapService coinMarketCap) =>
        new(context, coinMarketCap, NullLogger<GetCryptoAssetsQueryHandler>.Instance, new PassThroughCache());

    private static async Task<List<string>> VisibleSymbolsAsync(GetCryptoAssetsQueryHandler handler, bool hideZeroBalance)
    {
        var page = await handler.Handle(new GetCryptoAssetsQuery { UserId = 1, PageSize = 50, HideZeroBalance = hideZeroBalance }, CancellationToken.None);
        return page.Items.Single().CryptoAssetDto.Select(asset => asset.Symbol).ToList();
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
