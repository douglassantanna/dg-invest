using api.CoinMarketCap.Service;

namespace unit_tests.CryptoTests.Queries;

// "Hide zero and dust balances" on /cryptos hides assets worth less than US$1 (issue #329).
public class GetCryptoAssetsDustFilterTests
{
    private const int Btc = 1, Uma = 5617, Jup = 29210;

    [Fact]
    public async Task HideZeroBalance_HidesAssetsWorthLessThanOneDollarIncludingBalanceWithoutHistory()
    {
        await using var context = await AccountWithAssetsAsync();
        var handler = Handler(context, Prices(new() { [Btc] = 50_000m, [Uma] = 1.20m }));

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: true);

        // BTC worth $5; UMA (wallet dust without history) worth $0.0012; JUP has no price, so the old rule keeps it.
        visible.Should().BeEquivalentTo(["btc", "jup"]);
    }

    [Fact]
    public async Task HideZeroBalanceOff_ShowsEveryAsset()
    {
        await using var context = await AccountWithAssetsAsync();
        var handler = Handler(context, Prices(new() { [Btc] = 50_000m, [Uma] = 1.20m }));

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: false);

        visible.Should().BeEquivalentTo(["btc", "uma", "jup"]);
    }

    [Fact]
    public async Task HideZeroBalance_WhenCoinMarketCapFails_DoesNotEmptyTheList()
    {
        await using var context = await AccountWithAssetsAsync();
        // CoinMarketCap failed and no price is known yet: every price is absent.
        var handler = Handler(context, Prices(new(), fetchFailed: true));

        var visible = await VisibleSymbolsAsync(handler, hideZeroBalance: true);

        visible.Should().Contain("btc");
    }

    [Fact]
    public async Task AfterSwitchingAccount_ReturnsTheNewlySelectedAccount()
    {
        // Regression for the cached list that kept showing the previous account (#339): the list is no longer cached.
        await using var context = await AccountWithAssetsAsync();
        var brother = new Account("brother", 1);
        context.Accounts.Add(brother);
        await context.SaveChangesAsync();
        var handler = Handler(context, Prices(new() { [Btc] = 50_000m, [Uma] = 1.20m }));

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

    private static IPriceService Prices(Dictionary<int, decimal> prices, bool fetchFailed = false)
    {
        var priceService = new Mock<IPriceService>();
        priceService.Setup(p => p.GetPricesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceLookup(prices, fetchFailed));
        return priceService.Object;
    }

    private static GetCryptoAssetsQueryHandler Handler(DataContext context, IPriceService prices) => new(context, prices);

    private static async Task<List<string>> VisibleSymbolsAsync(GetCryptoAssetsQueryHandler handler, bool hideZeroBalance)
    {
        var page = await handler.Handle(new GetCryptoAssetsQuery { UserId = 1, PageSize = 50, HideZeroBalance = hideZeroBalance }, CancellationToken.None);
        return page.Items.Single().CryptoAssetDto.Select(asset => asset.Symbol).ToList();
    }
}
