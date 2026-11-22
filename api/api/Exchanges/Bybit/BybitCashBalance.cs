using System.Globalization;

namespace api.Exchanges.Bybit;

public static class BybitCashBalance
{
    public static readonly string[] CashCoins = ["USDT", "USDC"];
    private static readonly HashSet<string> CashCoinSet = new(CashCoins, StringComparer.OrdinalIgnoreCase);

    public static bool IsCashCoin(string symbol) => CashCoinSet.Contains(symbol);

    public static decimal FromAccountCoinBalance(BybitAccountCoinBalanceResponse response)
    {
        return ParseAccountCoinBalance(response.Result.Balance);
    }

    // The wallet balance is what the account owns, including funds locked in open orders; the transferable balance
    // excludes them and would make cash (or a coin) drop whenever a limit order is open.
    public static decimal ParseAccountCoinBalance(BybitAccountCoinBalance balance)
        => ParseAmount(string.IsNullOrWhiteSpace(balance.WalletBalance) ? balance.TransferBalance : balance.WalletBalance);

    public static IReadOnlyDictionary<string, decimal> SumWalletBalances(
        IEnumerable<BybitWalletBalanceResponse> responses)
    {
        var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var coin in responses.SelectMany(response => response.Result.List).SelectMany(account => account.Coin))
        {
            if (string.IsNullOrWhiteSpace(coin.Coin))
                continue;

            balances[coin.Coin] = balances.GetValueOrDefault(coin.Coin) + ParseAmount(coin.WalletBalance);
        }

        return balances;
    }

    private static decimal ParseAmount(string value) => decimal.TryParse(
        value,
        NumberStyles.Any,
        CultureInfo.InvariantCulture,
        out var parsed)
            ? parsed
            : 0;
}
