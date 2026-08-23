using api.Shared;

namespace api.Exchanges.Models;

public class LegacyBybitCredentialPromotion : Entity
{
    public int UserId { get; private set; }
    public string Exchange { get; private set; } = "Bybit";
    public int SourceAccountId { get; private set; }
    public string State { get; private set; } = "Pending";
    public string Outcome { get; private set; } = "Pending";
    public string? CredentialOperationId { get; private set; }
    public string? CredentialSetId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private LegacyBybitCredentialPromotion() { }
    public LegacyBybitCredentialPromotion(int userId, int sourceAccountId)
        => (UserId, SourceAccountId) = (userId, sourceAccountId);

    public void Record(string state, string outcome, string? operationId = null, string? credentialSetId = null)
    {
        State = state;
        Outcome = outcome;
        CredentialOperationId = operationId ?? CredentialOperationId;
        CredentialSetId = credentialSetId ?? CredentialSetId;
        UpdatedAt = DateTime.UtcNow;
    }
}
