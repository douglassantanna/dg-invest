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
    public bool IsEnabled { get; private set; } = true;
    public DateTime? LastVerifiedAt { get; private set; }
    public string Region { get; private set; } = "Global";

    private SyncStatus() { }

    public SyncStatus(int userId, int accountId, string exchangeName)
    {
        UserId = userId;
        AccountId = accountId;
        ExchangeName = exchangeName;
    }

    public void MarkConnected(string orderId, DateTime? checkpointAt = null)
    {
        Status = "Connected";
        var checkpoint = checkpointAt ?? DateTime.UtcNow;
        LastSyncAt = LastSyncAt is { } previousCheckpoint && previousCheckpoint > checkpoint
            ? previousCheckpoint
            : checkpoint;
        LastOrderId = orderId;
        ErrorCount = 0;
        LastErrorMessage = null;
    }

    public const string NotConfiguredStatus = "NotConfigured";

    // No API key/secret is stored for the account (e.g. a discovered sub-account nobody configured). Replaces any
    // stale error so the account shows "setup needed" instead of an old failure.
    public void MarkNotConfigured()
    {
        Status = NotConfiguredStatus;
        ErrorCount = 0;
        LastErrorMessage = null;
    }

    // A completed sync that skipped items: stays Connected, but the message is shown to the user.
    public void RecordWarning(string warningMessage) => LastErrorMessage = warningMessage;

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
    public void SetRegion(string? region) => Region = string.IsNullOrWhiteSpace(region) ? "Global" : region;
    public void EnableForCredentials()
    {
        IsEnabled = true;
        MarkCredentialsSet();
    }

    public void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }
    public void Disable()
    {
        IsEnabled = false;
        MarkDisconnected();
    }

    public void MarkVerified(DateTime? verifiedAt = null)
    {
        LastVerifiedAt = verifiedAt ?? DateTime.UtcNow;
    }
}
