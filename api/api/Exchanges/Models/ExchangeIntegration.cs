using api.Shared;

namespace api.Exchanges.Models;

public class ExchangeIntegration : Entity
{
    public int UserId { get; private set; }
    public string Exchange { get; private set; } = string.Empty;
    public string Status { get; private set; } = "NotSetup";
    public bool Enabled { get; private set; } = true;
    public DateTime? LastSyncAt { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public string? ActiveCredentialSetId { get; private set; }
    public Guid CredentialVersion { get; private set; } = Guid.NewGuid();

    private ExchangeIntegration() { }

    public ExchangeIntegration(int userId, string exchange)
    {
        UserId = userId;
        Exchange = exchange;
        CreatedDate = DateTime.UtcNow;
    }

    public void MarkEnabled() => Enabled = true;
    public void MarkDisabled() => Enabled = false;
    public void MarkDisconnected()
    {
        Enabled = false;
        Status = "Disconnected";
    }
    public void ToggleEnabled() => Enabled = !Enabled;
    public void MarkConfigured() => Status = "Configured";
    public void ActivateCredentialSet(string credentialSetId)
    {
        ActiveCredentialSetId = credentialSetId;
        CredentialVersion = Guid.NewGuid();
        MarkConfigured();
        MarkEnabled();
    }
    public void DeactivateCredentialSet()
    {
        ActiveCredentialSetId = null;
        CredentialVersion = Guid.NewGuid();
    }

    public void MarkSynced(DateTime timestamp)
    {
        Status = "Healthy";
        LastSyncAt = timestamp;
    }

    public void MarkError()
    {
        Status = "Error";
    }
}
