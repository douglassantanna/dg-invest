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

public class BybitAccountCoinBalancesResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitAccountCoinBalancesResult Result { get; set; } = new();
}

public class BybitAccountCoinBalancesResult
{
    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public List<BybitAccountCoinBalance> Balance { get; set; } = [];
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
