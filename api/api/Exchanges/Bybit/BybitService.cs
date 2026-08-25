using System.Security.Cryptography;
using System.Text;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace api.Exchanges.Bybit;

public class BybitService : IBybitService
{
    private const string SubMembersEndpoint = "/v5/user/submembers";
    private const string OrderHistoryEndpoint = "/v5/order/history";
    private const string DepositHistoryEndpoint = "/v5/asset/deposit/query-record";
    private const string WithdrawalHistoryEndpoint = "/v5/asset/withdraw/query-record";
    private const string AccountInfoEndpoint = "/v5/account/info";
    private const int RecvWindow = 60000;

    private readonly bool _useTestnet;
    private readonly ILogger<BybitService> _logger;

    public BybitService(IOptions<BybitSettings> settings, ILogger<BybitService> logger)
    {
        var bybitSettings = settings.Value;
        _useTestnet = bybitSettings.UseTestnet;
        _logger = logger;
    }

    private string GetBaseUrl(BybitRegion region) => BybitEndpoints.GetBaseUrl(region, _useTestnet);

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

    public async Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var paramStr = $"{timestamp}{apiKey}{RecvWindow}";
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var paramBytes = Encoding.UTF8.GetBytes(paramStr);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var baseUrl = GetBaseUrl(region);
            var response = await baseUrl
                .AppendPathSegment(SubMembersEndpoint)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitSubAccountResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetSubAccounts returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response.Result.SubMembers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit sub-accounts");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var paramStr = $"{timestamp}{apiKey}{RecvWindow}";
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var paramBytes = Encoding.UTF8.GetBytes(paramStr);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var baseUrl = GetBaseUrl(region);
            var response = await baseUrl
                .AppendPathSegment(AccountInfoEndpoint)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitAccountInfoResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogWarning("Bybit test connection returned error {Code}: {Message}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return true;
        }
        catch (BybitApiException)
        {
            throw;
        }
        catch (FlurlHttpException ex)
        {
            var errorBody = await ex.GetResponseStringAsync();
            _logger.LogWarning("Bybit test connection failed: {StatusCode} {Body}", ex.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Bybit connection");
            return false;
        }
    }

    public async Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var queryDict = new Dictionary<string, object> { ["category"] = "spot", ["limit"] = limit!.Value };
            if (startTime.HasValue)
                queryDict["startTime"] = startTime.Value;

            var queryParams = BuildQueryString(queryDict);
            var paramStr = $"{timestamp}{apiKey}{RecvWindow}{queryParams}";
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var paramBytes = Encoding.UTF8.GetBytes(paramStr);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var baseUrl = GetBaseUrl(region);
            var response = await baseUrl
                .AppendPathSegment(OrderHistoryEndpoint)
                .SetQueryParams(queryDict)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitOrderHistoryResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetOrderHistory returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response.Result.List;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit order history");
            throw;
        }
    }

    public async Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var queryDict = new Dictionary<string, object> { ["limit"] = limit!.Value };
            if (startTime.HasValue)
                queryDict["startTime"] = startTime.Value;

            var queryParams = BuildQueryString(queryDict);
            var paramStr = $"{timestamp}{apiKey}{RecvWindow}{queryParams}";
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var paramBytes = Encoding.UTF8.GetBytes(paramStr);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var baseUrl = GetBaseUrl(region);
            var response = await baseUrl
                .AppendPathSegment(DepositHistoryEndpoint)
                .SetQueryParams(queryDict)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitDepositHistoryResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetDepositHistory returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response.Result.Rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit deposit history");
            throw;
        }
    }

    public async Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var queryDict = new Dictionary<string, object> { ["limit"] = limit!.Value };
            if (startTime.HasValue)
                queryDict["startTime"] = startTime.Value;

            var queryParams = BuildQueryString(queryDict);
            var paramStr = $"{timestamp}{apiKey}{RecvWindow}{queryParams}";
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var paramBytes = Encoding.UTF8.GetBytes(paramStr);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(paramBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var baseUrl = GetBaseUrl(region);
            var response = await baseUrl
                .AppendPathSegment(WithdrawalHistoryEndpoint)
                .SetQueryParams(queryDict)
                .WithHeader("X-BAPI-API-KEY", apiKey)
                .WithHeader("X-BAPI-TIMESTAMP", timestamp)
                .WithHeader("X-BAPI-SIGN", signature)
                .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
                .GetJsonAsync<BybitWithdrawalHistoryResponse>();

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetWithdrawalHistory returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response.Result.Rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit withdrawal history");
            throw;
        }
    }

    private static string BuildQueryString(Dictionary<string, object> parameters)
    {
        return string.Join("&", parameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
    }
}
