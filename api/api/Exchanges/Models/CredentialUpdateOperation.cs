using api.Shared;

namespace api.Exchanges.Models;

public class CredentialUpdateOperation : Entity
{
    public string OperationId { get; private set; } = string.Empty;
    public int UserId { get; private set; }
    public string Exchange { get; private set; } = string.Empty;
    public int? AccountId { get; private set; }
    public bool CreatesAccount { get; private set; }
    public string State { get; private set; } = "Pending";
    public string? PreviousCredentialSetId { get; private set; }
    public Guid? PreviousCredentialVersion { get; private set; }
    public string NewCredentialSetId { get; private set; } = string.Empty;
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public Guid Version { get; private set; } = Guid.NewGuid();

    private CredentialUpdateOperation() { }
    public CredentialUpdateOperation(int userId, string exchange, int? accountId, string? previousCredentialSetId, Guid? previousCredentialVersion, bool createsAccount = false)
    {
        OperationId = Guid.NewGuid().ToString("N");
        NewCredentialSetId = Guid.NewGuid().ToString("N");
        UserId = userId;
        Exchange = exchange;
        AccountId = accountId;
        CreatesAccount = createsAccount;
        PreviousCredentialSetId = previousCredentialSetId;
        PreviousCredentialVersion = previousCredentialVersion;
    }
    public void MarkVaultWritten() => SetState("VaultWritten", null);
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }
    public void MarkActive() => SetState("Active", null);
    public void MarkRecoveryRequired(string error) => SetState("RecoveryRequired", error);
    public void MarkSuperseded() => SetState("Superseded", null);
    public void MarkRetired() => SetState("Retired", null);
    public void MarkCleaned() => SetState("Cleaned", null);
    private void SetState(string state, string? error)
    {
        State = state; Error = error; UpdatedAt = DateTime.UtcNow; Version = Guid.NewGuid();
    }
}
