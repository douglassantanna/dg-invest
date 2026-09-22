using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitInternalTransferResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitInternalTransferResult Result { get; set; } = new();
}

public class BybitInternalTransferResult
{
    [JsonPropertyName("list")]
    public List<BybitInternalTransferRow> List { get; set; } = [];

    [JsonPropertyName("nextPageCursor")]
    public string? NextPageCursor { get; set; }
}

public class BybitInternalTransferRow
{
    [JsonPropertyName("transferId")]
    public string TransferId { get; set; } = string.Empty;

    [JsonPropertyName("coin")]
    public string Coin { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("fromAccountType")]
    public string FromAccountType { get; set; } = string.Empty;

    [JsonPropertyName("toAccountType")]
    public string ToAccountType { get; set; } = string.Empty;

    [JsonPropertyName("fromMemberId")]
    public string FromMemberId { get; set; } = string.Empty;

    [JsonPropertyName("toMemberId")]
    public string ToMemberId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}
