namespace api.Exchanges.Bybit;
public interface IBybitService
{
    bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret);
    Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret);
}
