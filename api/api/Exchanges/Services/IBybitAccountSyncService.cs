using api.Cryptos.Models;
using api.Exchanges.Bybit;

namespace api.Exchanges.Services;

public interface IBybitAccountSyncService
{
    Task<BybitAccountSyncResult> SyncAsync(Account account, CancellationToken cancellationToken);
    Task<BybitAccountSyncResult> BackfillAsync(Account account, DateTime fromUtc, CancellationToken cancellationToken);
    Task<BybitAccountSyncResult> RecalculateAsync(Account account, CancellationToken cancellationToken);
}

public sealed record BybitAccountSyncResult(
    bool IsSuccess,
    bool IsSkipped,
    string Message,
    int StatusCode,
    BybitApiFailureKind FailureKind = BybitApiFailureKind.Other,
    string? ErrorCode = null,
    string? Endpoint = null)
{
    public static BybitAccountSyncResult Succeeded(string message) => new(true, false, message, 200);
    public static BybitAccountSyncResult Skipped(string message) => new(false, true, message, 400);
    public static BybitAccountSyncResult Failed(
        string message,
        int statusCode = 400,
        BybitApiFailureKind failureKind = BybitApiFailureKind.Other,
        string? errorCode = null,
        string? endpoint = null)
        => new(false, false, message, statusCode, failureKind, errorCode, endpoint);
}
