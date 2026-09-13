using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitWalletBalanceResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitWalletBalanceResult Result { get; set; } = new();
}

public class BybitWalletBalanceResult
{
    [JsonPropertyName("list")]
    public List<BybitWalletBalanceAccount> List { get; set; } = new();
}

public class BybitWalletBalanceAccount
{
    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("totalEquity")]
    public string TotalEquity { get; set; } = "0";

    [JsonPropertyName("coin")]
    public List<BybitWalletBalanceCoin> Coin { get; set; } = new();
}

public class BybitWalletBalanceCoin
{
    [JsonPropertyName("coin")]
    public string Coin { get; set; } = string.Empty;

    [JsonPropertyName("walletBalance")]
    public string WalletBalance { get; set; } = "0";

    [JsonPropertyName("availableBalance")]
    public string AvailableBalance { get; set; } = "0";

    [JsonPropertyName("usdValue")]
    public string UsdValue { get; set; } = "0";
}
