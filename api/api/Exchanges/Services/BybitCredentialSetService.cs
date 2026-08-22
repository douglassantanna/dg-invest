using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Services;

public static class BybitCredentialKeys
{
    public static string SetKey(string setId, string suffix) => $"bybit-set-{setId}-{suffix}";
    public static string LegacyAccountKey(int userId, int accountId, string suffix) => $"bybit-{userId}-{accountId}-{suffix}";
    public static string LegacyIntegrationKey(int userId, string suffix) => $"bybit-integration-{userId}-{suffix}";
    public static string Key(string? setId, int userId, int? accountId, string suffix) =>
        setId is not null ? SetKey(setId, suffix) : accountId is { } id ? LegacyAccountKey(userId, id, suffix) : LegacyIntegrationKey(userId, suffix);
}

public static class BybitCredentialReader
{
    public static async Task<KeyVaultSecretReadResult> ReadAsync(DataContext context, IKeyVaultService vault, int userId, int? accountId, string suffix, CancellationToken cancellationToken = default)
    {
        var setId = accountId is { } id
            ? await context.SyncStatuses.Where(x => x.UserId == userId && x.AccountId == id && x.ExchangeName == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken)
            : await context.ExchangeIntegrations.Where(x => x.UserId == userId && x.Exchange == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
        return await vault.GetSecretReadResultAsync(BybitCredentialKeys.Key(setId, userId, accountId, suffix));
    }
}

public interface IBybitCredentialSetService
{
    Task<CredentialUpdateResult> ReplaceAsync(int userId, int? accountId, IReadOnlyDictionary<string, string> replacements, CancellationToken cancellationToken);
    Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default);
    Task<int> ReconcileAsync(CancellationToken cancellationToken);
}

public record CredentialUpdateResult(bool Success, bool Unavailable, string? Error = null);

public class BybitCredentialSetService : IBybitCredentialSetService
{
    private static readonly string[] Suffixes = ["api-key", "api-secret", "webhook-secret"];
    private readonly DataContext _context;
    private readonly IKeyVaultService _vault;
    private readonly ILogger<BybitCredentialSetService> _logger;
    public BybitCredentialSetService(DataContext context, IKeyVaultService vault, ILogger<BybitCredentialSetService> logger)
        => (_context, _vault, _logger) = (context, vault, logger);

    public async Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default)
    {
        return await BybitCredentialReader.ReadAsync(_context, _vault, userId, accountId, suffix, cancellationToken);
    }

    public async Task<CredentialUpdateResult> ReplaceAsync(int userId, int? accountId, IReadOnlyDictionary<string, string> replacements, CancellationToken cancellationToken)
    {
        var priorSet = accountId is { } id
            ? await _context.SyncStatuses.Where(x => x.UserId == userId && x.AccountId == id && x.ExchangeName == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken)
            : await _context.ExchangeIntegrations.Where(x => x.UserId == userId && x.Exchange == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
        var operation = new CredentialUpdateOperation(userId, "Bybit", accountId, priorSet);
        _context.CredentialUpdateOperations.Add(operation);
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (Exception ex) { return new CredentialUpdateResult(false, false, ex.Message); }

        var values = new Dictionary<string, string>();
        foreach (var suffix in Suffixes)
        {
            if (replacements.TryGetValue(suffix, out var replacement)) { values[suffix] = replacement; continue; }
            var existing = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.Key(priorSet, userId, accountId, suffix));
            if (existing.IsUnavailable) { operation.MarkRecoveryRequired("Key Vault unavailable while reading previous set"); await TrySaveAsync(cancellationToken); return new(false, true); }
            values[suffix] = existing.Value ?? string.Empty;
        }
        try
        {
            foreach (var (suffix, value) in values)
                await _vault.SetSecretAsync(BybitCredentialKeys.SetKey(operation.NewCredentialSetId, suffix), value);
            operation.MarkVaultWritten();
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            operation.MarkRecoveryRequired(ex.Message);
            await TrySaveAsync(cancellationToken);
            _logger.LogError(ex, "Bybit credential operation {OperationId} did not finish Vault write", operation.OperationId);
            return new(false, false, ex.Message);
        }

        try
        {
            if (accountId is { } account)
            {
                var status = await _context.SyncStatuses.SingleOrDefaultAsync(x => x.UserId == userId && x.AccountId == account && x.ExchangeName == "Bybit", cancellationToken);
                if (status == null) { status = new SyncStatus(userId, account, "Bybit"); _context.SyncStatuses.Add(status); }
                status.ActivateCredentialSet(operation.NewCredentialSetId);
            }
            else
            {
                var integration = await _context.ExchangeIntegrations.SingleOrDefaultAsync(x => x.UserId == userId && x.Exchange == "Bybit", cancellationToken);
                if (integration == null) { integration = new ExchangeIntegration(userId, "Bybit"); _context.ExchangeIntegrations.Add(integration); }
                integration.ActivateCredentialSet(operation.NewCredentialSetId);
            }
            operation.MarkActive();
            await _context.SaveChangesAsync(cancellationToken);
            return new(true, false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            operation.MarkRecoveryRequired("Active credential set changed concurrently"); await TrySaveAsync(cancellationToken);
            return new(false, false, ex.Message);
        }
        catch (Exception ex)
        {
            // Do not restore immutable keys. A later process can determine whether activation committed.
            _context.ChangeTracker.Clear();
            var saved = await _context.CredentialUpdateOperations.SingleOrDefaultAsync(x => x.OperationId == operation.OperationId, cancellationToken);
            if (saved?.State == "Active") return new(true, false);
            if (saved != null) { saved.MarkRecoveryRequired(ex.Message); await TrySaveAsync(cancellationToken); }
            return new(false, false, ex.Message);
        }
    }

    public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
    {
        var operations = await _context.CredentialUpdateOperations.Where(x => x.State == "VaultWritten" || x.State == "RecoveryRequired").ToListAsync(cancellationToken);
        foreach (var operation in operations)
        {
            var active = operation.AccountId is { } account
                ? await _context.SyncStatuses.Where(x => x.UserId == operation.UserId && x.AccountId == account && x.ExchangeName == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken)
                : await _context.ExchangeIntegrations.Where(x => x.UserId == operation.UserId && x.Exchange == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
            if (active == operation.NewCredentialSetId) operation.MarkActive();
            else if (operation.State == "VaultWritten") operation.MarkRecoveryRequired("Activation was not observed");
        }
        await _context.SaveChangesAsync(cancellationToken);
        return operations.Count;
    }
    private async Task TrySaveAsync(CancellationToken cancellationToken) { try { await _context.SaveChangesAsync(cancellationToken); } catch (Exception ex) { _logger.LogError(ex, "Could not persist credential operation state"); } }
}
