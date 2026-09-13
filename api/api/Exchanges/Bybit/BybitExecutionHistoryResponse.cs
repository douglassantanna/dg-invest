using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitExecutionHistoryResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitExecutionHistoryResult Result { get; set; } = new();
}

public class BybitExecutionHistoryResult
{
    [JsonPropertyName("list")]
    public List<BybitExecutionData> List { get; set; } = new();
}
