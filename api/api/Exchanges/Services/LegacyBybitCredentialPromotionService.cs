using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Services;

public record LegacyBybitCredentialPromotionReport(int UserId, int? SourceAccountId, string Outcome, string State, string? CredentialOperationId, string? CredentialSetId);

public interface ILegacyBybitCredentialPromotionService
{
    Task<IReadOnlyList<LegacyBybitCredentialPromotionReport>> PromoteAsync(bool dryRun, CancellationToken cancellationToken);
}

public class LegacyBybitCredentialPromotionService : ILegacyBybitCredentialPromotionService
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _vault;
    private readonly IBybitCredentialSetService _credentials;

    public LegacyBybitCredentialPromotionService(DataContext context, IKeyVaultService vault, IBybitCredentialSetService credentials)
        => (_context, _vault, _credentials) = (context, vault, credentials);

    public async Task<IReadOnlyList<LegacyBybitCredentialPromotionReport>> PromoteAsync(bool dryRun, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts.Where(x => !x.IsDeleted)
            .Select(x => new { x.Id, x.UserId, x.Name, x.AccountType, x.Exchange, x.ExternalId }).ToListAsync(cancellationToken);
        var reports = new List<LegacyBybitCredentialPromotionReport>();
        foreach (var group in accounts.GroupBy(x => x.UserId))
        {
            var polluted = group.Where(x => x.AccountType == EAccountType.Manual && (!string.IsNullOrWhiteSpace(x.Exchange) || !string.IsNullOrWhiteSpace(x.ExternalId))).ToList();
            foreach (var account in polluted)
                reports.Add(new(group.Key, account.Id, "PollutedManualAccount", "Reported", null, null));

            var candidates = group.Where(x => x.Name == "main" && x.AccountType == EAccountType.Manual && string.IsNullOrWhiteSpace(x.Exchange) && string.IsNullOrWhiteSpace(x.ExternalId)).ToList();
            if (candidates.Count == 0) continue;
            if (candidates.Count != 1)
            {
                foreach (var account in candidates)
                    reports.Add(await RecordAsync(account.UserId, account.Id, "Conflict", "Conflict", dryRun, cancellationToken));
                continue;
            }

            var source = candidates[0];
            var existing = await _context.LegacyBybitCredentialPromotions.SingleOrDefaultAsync(x => x.UserId == source.UserId && x.Exchange == "Bybit", cancellationToken);
            if (existing is not null && existing.SourceAccountId != source.Id)
            {
                reports.Add(new(source.UserId, source.Id, "SourceConflict", "Conflict", existing.CredentialOperationId, existing.CredentialSetId));
                continue;
            }
            if (existing?.State == "Promoted")
            {
                reports.Add(new(source.UserId, existing.SourceAccountId, existing.Outcome, existing.State, existing.CredentialOperationId, existing.CredentialSetId));
                continue;
            }

            var integrationSet = await _context.ExchangeIntegrations.Where(x => x.UserId == source.UserId && x.Exchange == "Bybit")
                .Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
            if (integrationSet is not null)
            {
                if (existing?.CredentialOperationId is not null && existing.CredentialSetId == integrationSet
                    && await _context.CredentialUpdateOperations.AnyAsync(x => x.OperationId == existing.CredentialOperationId && x.NewCredentialSetId == integrationSet && x.State == "Active", cancellationToken))
                {
                    existing.Record("Promoted", "Promoted");
                    if (!dryRun) await _context.SaveChangesAsync(cancellationToken);
                    reports.Add(new(source.UserId, source.Id, existing.Outcome, existing.State, existing.CredentialOperationId, existing.CredentialSetId));
                    continue;
                }
                reports.Add(await RecordAsync(source.UserId, source.Id, "Conflict", "Conflict", dryRun, cancellationToken));
                continue;
            }

            var key = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(source.UserId, source.Id, "api-key"));
            var secret = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(source.UserId, source.Id, "api-secret"));
            if (key.IsUnavailable || secret.IsUnavailable)
            {
                reports.Add(await RecordAsync(source.UserId, source.Id, "Unavailable", "Unavailable", dryRun, cancellationToken));
                continue;
            }
            if (!key.IsFound || !secret.IsFound || string.IsNullOrWhiteSpace(key.Value) || string.IsNullOrWhiteSpace(secret.Value))
            {
                reports.Add(await RecordAsync(source.UserId, source.Id, "Incomplete", "Incomplete", dryRun, cancellationToken));
                continue;
            }
            var webhook = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(source.UserId, source.Id, "webhook-secret"));
            if (webhook.IsUnavailable)
            {
                reports.Add(await RecordAsync(source.UserId, source.Id, "Unavailable", "Unavailable", dryRun, cancellationToken));
                continue;
            }
            if (dryRun)
            {
                reports.Add(new(source.UserId, source.Id, "Ready", "DryRun", null, null));
                continue;
            }

            var pending = await GetOrCreatePendingAsync(existing, source.UserId, source.Id, cancellationToken);
            if (pending is null)
            {
                var current = await _context.LegacyBybitCredentialPromotions.AsNoTracking().SingleAsync(x => x.UserId == source.UserId && x.Exchange == "Bybit", cancellationToken);
                reports.Add(new(source.UserId, source.Id, current.Outcome, current.State, current.CredentialOperationId, current.CredentialSetId));
                continue;
            }
            var result = await _credentials.ReplaceAsync(source.UserId, null, new Dictionary<string, string>
            {
                ["api-key"] = key.Value!, ["api-secret"] = secret.Value!, ["webhook-secret"] = webhook.IsFound ? webhook.Value ?? string.Empty : string.Empty
            }, cancellationToken, verifyDestination: true, operationCreated: async operation =>
            {
                pending.Record("Pending", "Ready", operation.OperationId, operation.NewCredentialSetId);
                await _context.SaveChangesAsync(cancellationToken);
            });
            var outcome = result.Success ? "Promoted" : result.Unavailable ? "Unavailable" : "RecoveryRequired";
            var state = result.Success ? "Promoted" : result.Unavailable ? "Unavailable" : "RecoveryRequired";
            pending.Record(state, outcome, result.OperationId, result.CredentialSetId);
            await _context.SaveChangesAsync(cancellationToken);
            reports.Add(new(source.UserId, source.Id, outcome, state, result.OperationId, result.CredentialSetId));
        }
        return reports;
    }

    private async Task<LegacyBybitCredentialPromotionReport> RecordAsync(int userId, int sourceAccountId, string outcome, string state, bool dryRun, CancellationToken cancellationToken)
    {
        var promotion = await _context.LegacyBybitCredentialPromotions.SingleOrDefaultAsync(x => x.UserId == userId && x.Exchange == "Bybit", cancellationToken);
        if (promotion is not null && promotion.SourceAccountId != sourceAccountId)
            return new(userId, sourceAccountId, "SourceConflict", "Conflict", promotion.CredentialOperationId, promotion.CredentialSetId);
        if (dryRun) return new(userId, sourceAccountId, outcome, state, null, null);
        if (promotion is null) { promotion = new LegacyBybitCredentialPromotion(userId, sourceAccountId); _context.LegacyBybitCredentialPromotions.Add(promotion); }
        promotion.Record(state, outcome);
        await _context.SaveChangesAsync(cancellationToken);
        return new(userId, sourceAccountId, outcome, state, promotion.CredentialOperationId, promotion.CredentialSetId);
    }

    private async Task<LegacyBybitCredentialPromotion?> GetOrCreatePendingAsync(LegacyBybitCredentialPromotion? existing, int userId, int sourceAccountId, CancellationToken cancellationToken)
    {
        var promotion = existing ?? new LegacyBybitCredentialPromotion(userId, sourceAccountId);
        if (existing is null) _context.LegacyBybitCredentialPromotions.Add(promotion);
        promotion.Record("Pending", "Ready");
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return promotion;
        }
        catch (DbUpdateException) when (existing is null)
        {
            _context.ChangeTracker.Clear();
            return null;
        }
    }
}
