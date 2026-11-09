using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

// GET /v5/user/query-api. Works for master and sub-account keys.
public class BybitApiKeyInfoResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitApiKeyInfo Result { get; set; } = new();
}

public class BybitApiKeyInfo
{
    // 1 = read only; 0 = read and write (can trade and, on master keys, withdraw).
    [JsonPropertyName("readOnly")]
    public int ReadOnly { get; set; }

    public bool IsReadOnly => ReadOnly == 1;
}
