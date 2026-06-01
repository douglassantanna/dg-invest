using api.Exchanges.Bybit;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitSettingsTests
{
    [Fact]
    public void TradingBaseUrl_WhenUseTestnetIsFalse_ShouldReturnProduction()
    {
        var settings = new BybitSettings { UseTestnet = false };

        settings.TradingBaseUrl.Should().Be("https://api.bybit.com");
    }

    [Fact]
    public void TradingBaseUrl_WhenUseTestnetIsTrue_ShouldReturnTestnet()
    {
        var settings = new BybitSettings { UseTestnet = true };

        settings.TradingBaseUrl.Should().Be("https://api-testnet.bybit.com");
    }

    [Fact]
    public void AccountBaseUrl_ShouldAlwaysBeProduction()
    {
        var production = new BybitSettings { UseTestnet = false };
        var testnet = new BybitSettings { UseTestnet = true };

        production.AccountBaseUrl.Should().Be("https://api.bybit.com");
        testnet.AccountBaseUrl.Should().Be("https://api.bybit.com");
    }
}
