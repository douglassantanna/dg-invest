using System.Globalization;

namespace api.Exchanges.Bybit;

public static class BybitCashBalance
{
    public static readonly string[] CashCoins = ["USDT", "USDC"];
    private static readonly HashSet<string> CashCoinSet = new(CashCoins, StringComparer.OrdinalIgnoreCase);

    public static bool IsCashCoin(string symbol) => CashCoinSet.Contains(symbol);

    public static decimal SumStablecoinCash(BybitWalletBalanceResponse wallet)
    {
        var account = wallet.Result.List.FirstOrDefault();
        if (account is null)
            return 0;

        return account.Coin
            .Where(coin => CashCoinSet.Contains(coin.Coin))
            .Sum(coin => ParseAmount(string.IsNullOrWhiteSpace(coin.AvailableBalance) ? coin.WalletBalance : coin.AvailableBalance));
    }

    public static decimal FromAccountCoinBalance(BybitAccountCoinBalanceResponse response)
    {
        return ParseAccountCoinBalance(response.Result.Balance);
    }

    public static decimal ParseAccountCoinBalance(BybitAccountCoinBalance balance)
        => ParseAmount(string.IsNullOrWhiteSpace(balance.TransferBalance) ? balance.WalletBalance : balance.TransferBalance);

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
