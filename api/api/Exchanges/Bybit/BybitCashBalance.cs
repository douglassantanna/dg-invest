using System.Globalization;

namespace api.Exchanges.Bybit;

public static class BybitCashBalance
{
    private static readonly HashSet<string> CashCoins = new(StringComparer.OrdinalIgnoreCase) { "USDT", "USDC" };

    public static decimal SumStablecoinCash(BybitWalletBalanceResponse wallet)
    {
        var account = wallet.Result.List.FirstOrDefault();
        if (account is null)
            return 0;

        return account.Coin
            .Where(coin => CashCoins.Contains(coin.Coin))
            .Sum(coin => ParseAmount(string.IsNullOrWhiteSpace(coin.AvailableBalance) ? coin.WalletBalance : coin.AvailableBalance));
    }

    private static decimal ParseAmount(string value) => decimal.TryParse(
        value,
        NumberStyles.Any,
        CultureInfo.InvariantCulture,
        out var parsed)
            ? parsed
            : 0;
}
