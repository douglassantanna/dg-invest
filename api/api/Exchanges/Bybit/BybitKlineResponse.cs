using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

// GET /v5/market/kline (public). Each candle is [startTime, open, high, low, close, volume, turnover], newest first.
public class BybitKlineResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitKlineResult Result { get; set; } = new();
}

public class BybitKlineResult
{
    [JsonPropertyName("list")]
    public List<List<string>> List { get; set; } = [];
}
