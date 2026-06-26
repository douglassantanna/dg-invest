namespace api.Exchanges.Bybit;
public interface IBybitService
{
    bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret);
    Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret);
    Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null);
    Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null);
    Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null);
}
