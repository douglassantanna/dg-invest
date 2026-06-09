using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitOrderHistoryResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitOrderHistoryResult Result { get; set; } = new();
}

public class BybitOrderHistoryResult
{
    [JsonPropertyName("list")]
    public List<BybitOrderData> List { get; set; } = new();
}
