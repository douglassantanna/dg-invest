using System.Globalization;

namespace api.Exchanges.Bybit;

public static class BybitCashBalance
{
    public static readonly string[] CashCoins = ["USDT", "USDC"];
    private static readonly HashSet<string> CashCoinSet = new(CashCoins, StringComparer.OrdinalIgnoreCase);

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
        var balance = response.Result.Balance;
        return ParseAmount(string.IsNullOrWhiteSpace(balance.TransferBalance) ? balance.WalletBalance : balance.TransferBalance);
    }

    private static decimal ParseAmount(string value) => decimal.TryParse(
        value,
        NumberStyles.Any,
        CultureInfo.InvariantCulture,
        out var parsed)
            ? parsed
            : 0;
}
