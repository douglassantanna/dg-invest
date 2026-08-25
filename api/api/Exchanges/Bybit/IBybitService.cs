namespace api.Exchanges.Bybit;
public interface IBybitService
{
    bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret);
    Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global);
    Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null);
    Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null);
    Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null);
    Task<BybitWalletBalanceResponse> GetWalletBalanceAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, string accountType = "UNIFIED", string? memberId = null);
    Task<BybitAccountCoinBalanceResponse> GetAccountCoinBalanceAsync(string apiKey, string apiSecret, BybitRegion region, string accountType, string coin, string? memberId = null);
    Task<bool> TestConnectionAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global);
}
