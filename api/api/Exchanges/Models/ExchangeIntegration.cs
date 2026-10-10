using api.Shared;
using api.Cryptos.Models;

namespace api.Exchanges.Models;

public class ExchangeIntegration : Entity
{
    public const int TransportFailurePauseThreshold = 10;
    public const string AutoPausedStatus = "AutoPaused";
    public const int MaxPauseNotificationAttempts = 3;
    private static readonly TimeSpan PauseNotificationRetryDelay = TimeSpan.FromMinutes(1);

    public int UserId { get; private set; }
    public string Exchange { get; private set; } = string.Empty;
    public string Status { get; private set; } = "NotSetup";
    public bool Enabled { get; private set; } = true;
    public DateTime? LastSyncAt { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public string Region { get; private set; } = "Global";
    public int? MasterAccountId { get; private set; }
    public Account? MasterAccount { get; private set; }
    public int ConsecutiveTransportFailures { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string? LastErrorEndpoint { get; private set; }
    public int? LastErrorAccountId { get; private set; }
    public DateTime? LastErrorAt { get; private set; }
    public DateTime? AutoPausedAt { get; private set; }
    public int PauseNotificationAttempts { get; private set; }
    public DateTime? LastPauseNotificationAttemptAt { get; private set; }
    public DateTime? PauseNotificationSentAt { get; private set; }

    private ExchangeIntegration() { }

    public ExchangeIntegration(int userId, string exchange)
    {
        UserId = userId;
        Exchange = exchange;
        CreatedDate = DateTime.UtcNow;
    }

    public void MarkEnabled()
    {
        if (Status != AutoPausedStatus)
            Enabled = true;
    }
    public void MarkDisabled() => Enabled = false;
    public void MarkDisconnected()
    {
        Enabled = false;
        Status = "Disconnected";
        ClearPauseState();
    }
    public void ToggleEnabled()
    {
        if (Status != AutoPausedStatus)
            Enabled = !Enabled;
    }
    public void MarkConfigured()
    {
        if (Status != AutoPausedStatus)
            Status = "Configured";
    }
    public void SetRegion(string? region) => Region = string.IsNullOrWhiteSpace(region) ? "Global" : region;
    public void LinkMasterAccount(int accountId) => MasterAccountId = accountId;
    public void MarkSynced(DateTime timestamp)
    {
        MarkSyncCycleSucceeded(timestamp);
    }

    public void MarkError()
    {
        Status = "Error";
    }

    public bool RecordTransportFailure(string? errorCode, string errorMessage, string endpoint, int? accountId, DateTime occurredAt)
    {
        if (Status == AutoPausedStatus)
            return false;

        ConsecutiveTransportFailures++;
        SetLastError(errorCode, errorMessage, endpoint, accountId, occurredAt);
        if (ConsecutiveTransportFailures < TransportFailurePauseThreshold)
        {
            Status = "Error";
            return false;
        }

        AutoPause();
        return true;
    }

    public bool RecordPermanentCredentialFailure(string errorCode, string errorMessage, string endpoint, int? accountId, DateTime occurredAt)
    {
        if (Status == AutoPausedStatus)
            return false;

        ConsecutiveTransportFailures = 0;
        SetLastError(errorCode, errorMessage, endpoint, accountId, occurredAt);
        AutoPause();
        return true;
    }

    public void MarkSyncCycleSucceeded(DateTime timestamp)
    {
        if (Status == AutoPausedStatus)
            return;

        Status = "Healthy";
        LastSyncAt = timestamp;
        ConsecutiveTransportFailures = 0;
        LastErrorCode = null;
        LastErrorMessage = null;
        LastErrorEndpoint = null;
        LastErrorAccountId = null;
        LastErrorAt = null;
    }

    public void MarkNonTransportFailure(string errorMessage, int? accountId, DateTime occurredAt)
    {
        if (Status == AutoPausedStatus)
            return;

        Status = "Error";
        LastErrorCode = null;
        LastErrorMessage = errorMessage;
        LastErrorEndpoint = null;
        LastErrorAccountId = accountId;
        LastErrorAt = occurredAt;
    }

    public bool CanAttemptPauseNotification(DateTime now)
        => Status == AutoPausedStatus
            && PauseNotificationSentAt is null
            && PauseNotificationAttempts < MaxPauseNotificationAttempts
            && (LastPauseNotificationAttemptAt is null
                || LastPauseNotificationAttemptAt.Value <= now.Subtract(PauseNotificationRetryDelay));

    public void RecordPauseNotificationAttempt(DateTime attemptedAt, bool sent)
    {
        if (Status != AutoPausedStatus || PauseNotificationSentAt is not null)
            return;

        PauseNotificationAttempts++;
        LastPauseNotificationAttemptAt = attemptedAt;
        if (sent)
            PauseNotificationSentAt = attemptedAt;
    }

    private void SetLastError(string? errorCode, string errorMessage, string endpoint, int? accountId, DateTime occurredAt)
    {
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        LastErrorEndpoint = endpoint;
        LastErrorAccountId = accountId;
        LastErrorAt = occurredAt;
    }

    private void AutoPause()
    {
        Enabled = false;
        Status = AutoPausedStatus;
        AutoPausedAt = LastErrorAt ?? DateTime.UtcNow;
        PauseNotificationAttempts = 0;
        LastPauseNotificationAttemptAt = null;
        PauseNotificationSentAt = null;
    }

    private void ClearPauseState()
    {
        ConsecutiveTransportFailures = 0;
        LastErrorCode = null;
        LastErrorMessage = null;
        LastErrorEndpoint = null;
        LastErrorAccountId = null;
        LastErrorAt = null;
        AutoPausedAt = null;
        PauseNotificationAttempts = 0;
        LastPauseNotificationAttemptAt = null;
        PauseNotificationSentAt = null;
    }
}
