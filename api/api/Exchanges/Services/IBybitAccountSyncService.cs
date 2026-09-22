using api.Cryptos.Models;

namespace api.Exchanges.Services;

public interface IBybitAccountSyncService
{
    Task<BybitAccountSyncResult> SyncAsync(Account account, CancellationToken cancellationToken);
}

public sealed record BybitAccountSyncResult(bool IsSuccess, bool IsSkipped, string Message, int StatusCode)
{
    public static BybitAccountSyncResult Succeeded(string message) => new(true, false, message, 200);
    public static BybitAccountSyncResult Skipped(string message) => new(false, true, message, 400);
    public static BybitAccountSyncResult Failed(string message, int statusCode = 400) => new(false, false, message, statusCode);
}
