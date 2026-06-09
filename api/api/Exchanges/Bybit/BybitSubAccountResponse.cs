using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitSubAccountResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitSubAccountResult Result { get; set; } = new();
}

public class BybitSubAccountResult
{
    [JsonPropertyName("subMembers")]
    public List<BybitSubMember> SubMembers { get; set; } = new();
}
