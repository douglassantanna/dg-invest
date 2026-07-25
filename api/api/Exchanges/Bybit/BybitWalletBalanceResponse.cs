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
    public List<BybitWalletAccount> List { get; set; } = new();
}

public class BybitWalletAccount
{
    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("totalEquity")]
    public string TotalEquity { get; set; } = "0";
}
