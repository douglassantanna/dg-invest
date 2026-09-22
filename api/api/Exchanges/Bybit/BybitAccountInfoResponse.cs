using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitAccountInfoResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;
}
