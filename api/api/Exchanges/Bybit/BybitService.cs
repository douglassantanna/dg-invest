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
    private const string ExecutionHistoryEndpoint = "/v5/execution/list";
    private const string DepositHistoryEndpoint = "/v5/asset/deposit/query-record";
    private const string WithdrawalHistoryEndpoint = "/v5/asset/withdraw/query-record";
    private const string InternalTransferHistoryEndpoint = "/v5/asset/transfer/query-inter-transfer-list";
    private const string UniversalTransferHistoryEndpoint = "/v5/asset/transfer/query-universal-transfer-list";
    private const string WalletBalanceEndpoint = "/v5/account/wallet-balance";
    private const string AccountCoinBalanceEndpoint = "/v5/asset/transfer/query-account-coin-balance";
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
            var response = await SendPrivateGetAsync<BybitSubAccountResponse>(
                apiKey, apiSecret, region, SubMembersEndpoint, new Dictionary<string, object>());

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
            var response = await SendPrivateGetAsync<BybitAccountInfoResponse>(
                apiKey, apiSecret, region, AccountInfoEndpoint, new Dictionary<string, object>());

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

    public async Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object>
                    {
                        ["category"] = "spot",
                        ["limit"] = limit ?? 50
                    };
                    if (startTime.HasValue)
                        query["startTime"] = startTime.Value;
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitOrderHistoryResponse>(
                        apiKey, apiSecret, region, OrderHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.List, response.Result.NextPageCursor),
                "order history", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit order history");
            throw;
        }
    }

    public async Task<List<BybitExecutionData>> GetExecutionHistoryAsync(string apiKey, string apiSecret, BybitRegion region, string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object>
                    {
                        ["category"] = "spot",
                        ["limit"] = 100,
                        ["orderId"] = orderId
                    };
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitExecutionHistoryResponse>(
                        apiKey, apiSecret, region, ExecutionHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.List, response.Result.NextPageCursor),
                $"execution history for order {orderId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit execution history for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object> { ["limit"] = limit ?? 50 };
                    if (startTime.HasValue)
                        query["startTime"] = startTime.Value;
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitDepositHistoryResponse>(
                        apiKey, apiSecret, region, DepositHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.Rows, response.Result.NextPageCursor),
                "deposit history", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit deposit history");
            throw;
        }
    }

    public async Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object> { ["limit"] = limit ?? 50 };
                    if (startTime.HasValue)
                        query["startTime"] = startTime.Value;
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitWithdrawalHistoryResponse>(
                        apiKey, apiSecret, region, WithdrawalHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.Rows, response.Result.NextPageCursor),
                "withdrawal history", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit withdrawal history");
            throw;
        }
    }

    public async Task<List<BybitInternalTransferRow>> GetInternalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object> { ["limit"] = limit ?? 50 };
                    if (startTime.HasValue)
                        query["startTime"] = startTime.Value;
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitInternalTransferResponse>(
                        apiKey, apiSecret, region, InternalTransferHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.List, response.Result.NextPageCursor),
                "internal transfer history", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit internal transfer history");
            throw;
        }
    }

    public async Task<List<BybitInternalTransferRow>> GetUniversalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPagedAsync(
                cursor =>
                {
                    var query = new Dictionary<string, object> { ["limit"] = limit ?? 50 };
                    if (startTime.HasValue)
                        query["startTime"] = startTime.Value;
                    if (!string.IsNullOrWhiteSpace(cursor))
                        query["cursor"] = cursor;

                    return SendPrivateGetAsync<BybitInternalTransferResponse>(
                        apiKey, apiSecret, region, UniversalTransferHistoryEndpoint, query, cancellationToken);
                },
                response => (response.RetCode, response.RetMsg, response.Result.List, response.Result.NextPageCursor),
                "universal transfer history", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit universal transfer history");
            throw;
        }
    }

    public async Task<BybitWalletBalanceResponse> GetWalletBalanceAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, string accountType = "UNIFIED", string? memberId = null)
    {
        try
        {
            var queryDict = new Dictionary<string, object> { ["accountType"] = accountType };
            if (!string.IsNullOrWhiteSpace(memberId))
                queryDict["memberId"] = memberId;
            var response = await SendPrivateGetAsync<BybitWalletBalanceResponse>(
                apiKey, apiSecret, region, WalletBalanceEndpoint, queryDict);

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetWalletBalance returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit wallet balance");
            throw;
        }
    }

    public async Task<BybitAccountCoinBalanceResponse> GetAccountCoinBalanceAsync(string apiKey, string apiSecret, BybitRegion region, string accountType, string coin, string? memberId = null)
    {
        try
        {
            var queryDict = new Dictionary<string, object> { ["accountType"] = accountType, ["coin"] = coin };
            if (!string.IsNullOrWhiteSpace(memberId))
                queryDict["memberId"] = memberId;
            var response = await SendPrivateGetAsync<BybitAccountCoinBalanceResponse>(
                apiKey, apiSecret, region, AccountCoinBalanceEndpoint, queryDict);

            if (response.RetCode != 0)
            {
                _logger.LogError("Bybit GetAccountCoinBalance returned error {Code}: {Msg}", response.RetCode, response.RetMsg);
                throw new BybitApiException(response.RetCode, response.RetMsg);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bybit account coin balance");
            throw;
        }
    }

    private async Task<TResponse> SendPrivateGetAsync<TResponse>(
        string apiKey,
        string apiSecret,
        BybitRegion region,
        string endpoint,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var url = GetBaseUrl(region).AppendPathSegment(endpoint);
        foreach (var parameter in parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal))
            url = url.SetQueryParam(parameter.Key, parameter.Value);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var payload = $"{timestamp}{apiKey}{RecvWindow}{url.Query}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        return await url
            .WithHeader("X-BAPI-API-KEY", apiKey)
            .WithHeader("X-BAPI-TIMESTAMP", timestamp)
            .WithHeader("X-BAPI-SIGN", signature)
            .WithHeader("X-BAPI-RECV-WINDOW", RecvWindow.ToString())
            .GetJsonAsync<TResponse>(cancellationToken);
    }

    private static async Task<List<TItem>> GetPagedAsync<TResponse, TItem>(
        Func<string?, Task<TResponse>> fetchPage,
        Func<TResponse, (int RetCode, string RetMsg, List<TItem> Items, string? NextCursor)> readPage,
        string operation,
        CancellationToken cancellationToken)
    {
        var items = new List<TItem>();
        var seenCursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await fetchPage(cursor);
            var pageResult = readPage(response);
            if (pageResult.RetCode != 0)
                throw new BybitApiException(pageResult.RetCode, pageResult.RetMsg);

            items.AddRange(pageResult.Items);
            if (string.IsNullOrWhiteSpace(pageResult.NextCursor))
                return items;

            if (!seenCursors.Add(pageResult.NextCursor!))
                throw new InvalidOperationException($"Bybit {operation} returned a repeated page cursor");

            cursor = pageResult.NextCursor;
        }
    }
}
