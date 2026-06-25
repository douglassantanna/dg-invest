using api.Shared;

namespace api.Exchanges.Models;

public class SyncStatus : Entity
{
    public int UserId { get; private set; }
    public int AccountId { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public DateTime? LastSyncAt { get; private set; }
    public string? LastOrderId { get; private set; }
    public string Status { get; private set; } = "Disconnected";
    public int ErrorCount { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTime? BybitCredentialsSetAt { get; private set; }

    private SyncStatus() { }

    public SyncStatus(int userId, int accountId, string exchangeName)
    {
        UserId = userId;
        AccountId = accountId;
        ExchangeName = exchangeName;
    }

    public void MarkConnected(string orderId)
    {
        Status = "Connected";
        LastSyncAt = DateTime.UtcNow;
        LastOrderId = orderId;
        ErrorCount = 0;
        LastErrorMessage = null;
    }

    public void MarkError(string? errorMessage = null)
    {
        Status = "Error";
        ErrorCount++;
        LastErrorMessage = errorMessage;
    }

    public void MarkDisconnected(string? errorMessage = null)
    {
        Status = "Disconnected";
        LastErrorMessage = errorMessage;
    }

    public void MarkCredentialsSet()
    {
        BybitCredentialsSetAt ??= DateTime.UtcNow;
    }
}
