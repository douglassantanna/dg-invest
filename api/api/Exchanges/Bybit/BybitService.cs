using System.Security.Cryptography;
using System.Text;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace api.Exchanges.Bybit;

public class BybitService : IBybitService
{
    private const string SubMembersEndpoint = "/v5/user/submembers";
    private const int RecvWindow = 5000;

    private readonly string _tradingBaseUrl;
    private readonly string _accountBaseUrl;
    private readonly ILogger<BybitService> _logger;

    public BybitService(IOptions<BybitSettings> settings, ILogger<BybitService> logger)
    {
        _tradingBaseUrl = settings.Value.TradingBaseUrl;
        _accountBaseUrl = settings.Value.AccountBaseUrl;
        _logger = logger;
        _logger.LogInformation("BybitService initialised — trading: {TradingUrl} | accounts: {AccountUrl}",
            _tradingBaseUrl, _accountBaseUrl);
    }

    // Bybit webhook signature: HMAC-SHA256(webhookSecret, timestamp + rawBody), hex-encoded.
    // Header names: X-Bybit-Timestamp, X-Bybit-Signature.
    public bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret)
    {
        try
        {
            var payload = $"{timestamp}{rawBody}";
            var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var expectedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Bybit webhook signature");
            return false;
        }
    }

    public async Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var signature = BuildApiSignature(apiKey, apiSecret, timestamp, string.Empty);

            var response = await _accountBaseUrl
                .AppendPathSegment(SubMembersEndpoint)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitSubAccountResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetSubAccounts returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                return new List<BybitSubMember>();
            }

            return response.Result.SubMembers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit sub-accounts");
            throw;
        }
    }

    // Bybit REST API signature: HMAC-SHA256(apiSecret, timestamp + apiKey + recvWindow + queryString).
    private static string BuildApiSignature(string apiKey, string apiSecret, string timestamp, string queryString)
    {
        var paramStr = $"{timestamp}{apiKey}{RecvWindow}{queryString}";
        var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
        var paramBytes = Encoding.UTF8.GetBytes(paramStr);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(paramBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
