using api.Exchanges.Bybit;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitSettingsTests
{
    [Fact]
    public void UseTestnet_DefaultsToFalse()
    {
        var settings = new BybitSettings();

        settings.UseTestnet.Should().BeFalse();
    }

    [Fact]
    public void UseTestnet_CanBeSetToTrue()
    {
        var settings = new BybitSettings { UseTestnet = true };

        settings.UseTestnet.Should().BeTrue();
    }
}
