using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitAccountCoinBalanceResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitAccountCoinBalanceResult Result { get; set; } = new();
}

public class BybitAccountCoinBalanceResult
{
    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public BybitAccountCoinBalance Balance { get; set; } = new();
}

public class BybitAccountCoinBalance
{
    [JsonPropertyName("coin")]
    public string Coin { get; set; } = string.Empty;

    [JsonPropertyName("walletBalance")]
    public string WalletBalance { get; set; } = "0";

    [JsonPropertyName("transferBalance")]
    public string TransferBalance { get; set; } = "0";
}
