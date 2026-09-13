using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BybitRegion
{
    Global = 0,
    Eu = 1
}

public static class BybitEndpoints
{
    public static string GetBaseUrl(BybitRegion region, bool useTestnet)
    {
        var host = region switch
        {
            BybitRegion.Eu => useTestnet ? "api-testnet.bybit.eu" : "api.bybit.eu",
            _ => useTestnet ? "api-testnet.bybit.com" : "api.bybit.com"
        };
        return $"https://{host}";
    }

    public static BybitRegion Parse(string? value) =>
        Enum.TryParse<BybitRegion>(value, ignoreCase: true, out var region) ? region : BybitRegion.Global;
}
