namespace api.Exchanges.Bybit;
public interface IBybitService
{
    Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global);
    Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default, long? endTime = null);
    // Spot fills executed in [startTime, endTime) (at most 7 days), all pages.
    Task<List<BybitExecutionData>> GetExecutionsAsync(string apiKey, string apiSecret, BybitRegion region, long startTime, long endTime, CancellationToken cancellationToken = default);
    Task<List<BybitExecutionData>> GetExecutionHistoryAsync(string apiKey, string apiSecret, BybitRegion region, string orderId, CancellationToken cancellationToken = default);
    Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default, long? endTime = null);
    Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default, long? endTime = null);
    Task<List<BybitInternalTransferRow>> GetInternalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default, long? endTime = null);
    Task<List<BybitInternalTransferRow>> GetUniversalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null, CancellationToken cancellationToken = default, long? endTime = null);
    Task<BybitWalletBalanceResponse> GetWalletBalanceAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, string accountType = "UNIFIED", string? memberId = null);
    Task<BybitAccountCoinBalanceResponse> GetAccountCoinBalanceAsync(string apiKey, string apiSecret, BybitRegion region, string accountType, string coin, string? memberId = null);
    Task<BybitAccountCoinBalancesResponse> GetAccountCoinBalancesAsync(string apiKey, string apiSecret, BybitRegion region, string accountType, string? memberId = null);
    Task<bool> TestConnectionAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global);
    // Spot price of {coin}USDT at a past moment, from Bybit's public 1-minute candles. Null when Bybit has no such pair or candle.
    Task<decimal?> GetSpotPriceAtAsync(string coin, DateTimeOffset at, CancellationToken cancellationToken = default);
    Task<BybitApiKeyInfo> GetApiKeyInfoAsync(string apiKey, string apiSecret, BybitRegion region, CancellationToken cancellationToken = default);
}
