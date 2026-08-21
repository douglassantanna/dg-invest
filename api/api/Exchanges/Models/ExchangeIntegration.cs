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

    private ExchangeIntegration() { }

    public ExchangeIntegration(int userId, string exchange)
    {
        UserId = userId;
        Exchange = exchange;
        CreatedDate = DateTime.UtcNow;
    }

    public void MarkEnabled() => Enabled = true;
    public void MarkDisabled() => Enabled = false;
    public void ToggleEnabled() => Enabled = !Enabled;

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
