using api.Exchanges.Bybit;
using FluentAssertions;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitCashBalanceTests
{
    [Theory]
    [InlineData("USDT", true)]
    [InlineData("usdc", true)]
    [InlineData("ETH", false)]
    public void IsCashCoin_ShouldClassifyStablecoins(string symbol, bool expected)
    {
        BybitCashBalance.IsCashCoin(symbol).Should().Be(expected);
    }

    [Fact]
    public void SumWalletBalances_ShouldAggregateCoinsAcrossWalletAccounts()
    {
        var responses = new[]
        {
            new BybitWalletBalanceResponse
            {
                Result = new BybitWalletBalanceResult
                {
                    List =
                    [
                        new BybitWalletBalanceAccount
                        {
                            Coin =
                            [
                                new BybitWalletBalanceCoin { Coin = "ETH", WalletBalance = "1.25" },
                                new BybitWalletBalanceCoin { Coin = "USDT", WalletBalance = "100" }
                            ]
                        }
                    ]
                }
            },
            new BybitWalletBalanceResponse
            {
                Result = new BybitWalletBalanceResult
                {
                    List =
                    [
                        new BybitWalletBalanceAccount
                        {
                            Coin =
                            [
                                new BybitWalletBalanceCoin { Coin = "eth", WalletBalance = "0.75" }
                            ]
                        }
                    ]
                }
            }
        };

        var balances = BybitCashBalance.SumWalletBalances(responses);

        balances["ETH"].Should().Be(2m);
        balances["USDT"].Should().Be(100m);
    }
}
