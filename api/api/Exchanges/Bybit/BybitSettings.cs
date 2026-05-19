namespace api.Exchanges.Bybit;
public class BybitSettings
{
    public bool UseTestnet { get; set; } = false;

    // Used for trading/webhook-related calls — switches to testnet when enabled.
    public string TradingBaseUrl => UseTestnet
        ? "https://api-testnet.bybit.com"
        : "https://api.bybit.com";

    // Account management endpoints (e.g. sub-members) are not available on testnet.
    public string AccountBaseUrl => "https://api.bybit.com";
}
