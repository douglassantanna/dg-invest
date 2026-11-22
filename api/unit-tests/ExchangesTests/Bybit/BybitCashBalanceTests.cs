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
    public void ParseAccountCoinBalance_ShouldUseTheWalletBalanceIncludingFundsLockedInOpenOrders()
    {
        // 100 USDT held, 20 locked in an open limit order: the portfolio still owns 100, only 80 is transferable.
        var balance = new BybitAccountCoinBalance { Coin = "USDT", WalletBalance = "100", TransferBalance = "80" };

        BybitCashBalance.ParseAccountCoinBalance(balance).Should().Be(100m);
    }

    [Fact]
    public void ParseAccountCoinBalance_WhenWalletBalanceIsMissing_ShouldFallBackToTransferBalance()
    {
        var balance = new BybitAccountCoinBalance { Coin = "USDT", WalletBalance = "", TransferBalance = "80" };

        BybitCashBalance.ParseAccountCoinBalance(balance).Should().Be(80m);
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
