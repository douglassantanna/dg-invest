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
        if (accountId is not { } id)
        {
            var integrationSetId = await context.ExchangeIntegrations.Where(x => x.UserId == userId && x.Exchange == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
            return await vault.GetSecretReadResultAsync(BybitCredentialKeys.Key(integrationSetId, userId, null, suffix));
        }

        // A status with no pointer is deliberately revoked, not a legacy credential fallback.
        var pointer = await context.SyncStatuses.Where(x => x.UserId == userId && x.AccountId == id && x.ExchangeName == "Bybit")
            .Select(x => new { x.ActiveCredentialSetId })
            .SingleOrDefaultAsync(cancellationToken);
        if (pointer is not null && pointer.ActiveCredentialSetId is null && !await context.Accounts.AnyAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, cancellationToken))
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound);
        var setId = pointer?.ActiveCredentialSetId;
        return await vault.GetSecretReadResultAsync(BybitCredentialKeys.Key(setId, userId, accountId, suffix));
    }
}

public interface IBybitCredentialSetService
{
    Task<CredentialUpdateResult> ReplaceAsync(int userId, int? accountId, IReadOnlyDictionary<string, string> replacements, CancellationToken cancellationToken, bool createsAccount = false, bool verifyDestination = false, Func<CredentialUpdateOperation, Task>? operationCreated = null);
    Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default);
    Task<int> ReconcileAsync(CancellationToken cancellationToken);
}

public record CredentialUpdateResult(bool Success, bool Unavailable, string? Error = null, string? OperationId = null, string? CredentialSetId = null);

public class BybitCredentialSetService : IBybitCredentialSetService
{
    private static readonly string[] Suffixes = ["api-key", "api-secret", "webhook-secret"];
    private static readonly TimeSpan PendingOperationGracePeriod = TimeSpan.FromMinutes(10);
    private readonly DataContext _context;
    private readonly IKeyVaultService _vault;
    private readonly ILogger<BybitCredentialSetService> _logger;
    public BybitCredentialSetService(DataContext context, IKeyVaultService vault, ILogger<BybitCredentialSetService> logger)
        => (_context, _vault, _logger) = (context, vault, logger);

    public async Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default)
    {
        return await BybitCredentialReader.ReadAsync(_context, _vault, userId, accountId, suffix, cancellationToken);
    }

    public async Task<CredentialUpdateResult> ReplaceAsync(int userId, int? accountId, IReadOnlyDictionary<string, string> replacements, CancellationToken cancellationToken, bool createsAccount = false, bool verifyDestination = false, Func<CredentialUpdateOperation, Task>? operationCreated = null)
    {
        string? priorSet;
        Guid? priorVersion;
        if (accountId is { } id)
        {
            var pointer = await _context.SyncStatuses.Where(x => x.UserId == userId && x.AccountId == id && x.ExchangeName == "Bybit")
                .Select(x => new { x.ActiveCredentialSetId, x.CredentialVersion }).SingleOrDefaultAsync(cancellationToken);
            priorSet = pointer?.ActiveCredentialSetId;
            priorVersion = pointer?.CredentialVersion;
        }
        else
        {
            var pointer = await _context.ExchangeIntegrations.Where(x => x.UserId == userId && x.Exchange == "Bybit")
                .Select(x => new { x.ActiveCredentialSetId, x.CredentialVersion }).SingleOrDefaultAsync(cancellationToken);
            priorSet = pointer?.ActiveCredentialSetId;
            priorVersion = pointer?.CredentialVersion;
        }
        var operation = new CredentialUpdateOperation(userId, "Bybit", accountId, priorSet, priorVersion, createsAccount);
        _context.CredentialUpdateOperations.Add(operation);
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (Exception ex) { return new CredentialUpdateResult(false, false, ex.Message); }
        try
        {
            if (operationCreated is not null) await operationCreated(operation);
        }
        catch (Exception ex)
        {
            operation.MarkRecoveryRequired("Could not persist credential operation correlation");
            await TrySaveAsync(cancellationToken);
            return new(false, false, ex.Message, operation.OperationId, operation.NewCredentialSetId);
        }

        var values = new Dictionary<string, string>();
        foreach (var suffix in Suffixes)
        {
            if (replacements.TryGetValue(suffix, out var replacement)) { values[suffix] = replacement; continue; }
            var existing = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.Key(priorSet, userId, accountId, suffix));
            if (existing.IsUnavailable) { operation.MarkRecoveryRequired("Key Vault unavailable while reading previous set"); await TrySaveAsync(cancellationToken); return new(false, true, OperationId: operation.OperationId, CredentialSetId: operation.NewCredentialSetId); }
            values[suffix] = existing.Value ?? string.Empty;
        }
        try
        {
            foreach (var (suffix, value) in values)
            {
                await _vault.SetSecretAsync(BybitCredentialKeys.SetKey(operation.NewCredentialSetId, suffix), value);
                operation.Touch();
                await _context.SaveChangesAsync(cancellationToken);
            }
            operation.MarkVaultWritten();
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            operation.MarkRecoveryRequired(ex.Message);
            await TrySaveAsync(cancellationToken);
            _logger.LogError(ex, "Bybit credential operation {OperationId} did not finish Vault write", operation.OperationId);
            return new(false, false, "Credential set write failed", operation.OperationId, operation.NewCredentialSetId);
        }

        if (verifyDestination) foreach (var suffix in Suffixes)
        {
            var written = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.SetKey(operation.NewCredentialSetId, suffix));
            if (written.IsUnavailable)
            {
                operation.MarkRecoveryRequired("Key Vault unavailable while verifying credential set");
                await TrySaveAsync(cancellationToken);
                return new(false, true, OperationId: operation.OperationId, CredentialSetId: operation.NewCredentialSetId);
            }
            if (!written.IsFound || written.Value != values[suffix])
            {
                operation.MarkRecoveryRequired("Credential set verification failed");
                await TrySaveAsync(cancellationToken);
                return new(false, false, "Credential set verification failed", operation.OperationId, operation.NewCredentialSetId);
            }
        }

        try
        {
            var activated = true;
            if (priorVersion is { } accountVersion && accountId is { } account)
            {
                var now = DateTime.UtcNow;
                activated = await _context.SyncStatuses
                    .Where(x => x.UserId == userId && x.AccountId == account && x.ExchangeName == "Bybit" && x.ActiveCredentialSetId == priorSet && x.CredentialVersion == accountVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ActiveCredentialSetId, operation.NewCredentialSetId)
                        .SetProperty(x => x.CredentialVersion, Guid.NewGuid())
                        .SetProperty(x => x.BybitCredentialsSetAt, x => x.BybitCredentialsSetAt ?? now), cancellationToken) == 1;
            }
            else if (priorVersion is { } integrationVersion)
            {
                activated = await _context.ExchangeIntegrations
                    .Where(x => x.UserId == userId && x.Exchange == "Bybit" && x.ActiveCredentialSetId == priorSet && x.CredentialVersion == integrationVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ActiveCredentialSetId, operation.NewCredentialSetId)
                        .SetProperty(x => x.CredentialVersion, Guid.NewGuid())
                        .SetProperty(x => x.Status, "Configured"), cancellationToken) == 1;
            }
            else if (accountId is { } accountToCreate)
            {
                var status = new SyncStatus(userId, accountToCreate, "Bybit");
                status.ActivateCredentialSet(operation.NewCredentialSetId);
                _context.SyncStatuses.Add(status);
            }
            else
            {
                var integration = new ExchangeIntegration(userId, "Bybit");
                integration.ActivateCredentialSet(operation.NewCredentialSetId);
                _context.ExchangeIntegrations.Add(integration);
            }
            if (!activated)
            {
                operation.MarkRecoveryRequired("Active credential set changed concurrently");
                await TrySaveAsync(cancellationToken);
                return new(false, false, "Active credential set changed concurrently", operation.OperationId, operation.NewCredentialSetId);
            }
            operation.MarkActive();
            await _context.SaveChangesAsync(cancellationToken);
            return new(true, false, OperationId: operation.OperationId, CredentialSetId: operation.NewCredentialSetId);
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
        var pendingCutoff = DateTime.UtcNow - PendingOperationGracePeriod;
        var operations = await _context.CredentialUpdateOperations
            .Where(x => x.State == "VaultWritten" || x.State == "RecoveryRequired" || (x.State == "Pending" && x.UpdatedAt <= pendingCutoff))
            .ToListAsync(cancellationToken);
        foreach (var operation in operations)
        {
            var keysComplete = true;
            var vaultUnavailable = false;
            foreach (var suffix in Suffixes)
            {
                var secret = await _vault.GetSecretReadResultAsync(BybitCredentialKeys.SetKey(operation.NewCredentialSetId, suffix));
                if (secret.IsUnavailable)
                {
                    vaultUnavailable = true;
                    continue;
                }
                if (!secret.IsFound)
                {
                    keysComplete = false;
                }
            }
            // Inspect every immutable key before deciding whether a crash left a recoverable set.
            if (vaultUnavailable)
            {
                operation.MarkRecoveryRequired("Key Vault unavailable while verifying credential set");
                continue;
            }
            if (!keysComplete)
            {
                operation.MarkCleaned();
                continue;
            }

            var active = operation.AccountId is { } account
                ? await _context.SyncStatuses.Where(x => x.UserId == operation.UserId && x.AccountId == account && x.ExchangeName == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken)
                : await _context.ExchangeIntegrations.Where(x => x.UserId == operation.UserId && x.Exchange == "Bybit").Select(x => x.ActiveCredentialSetId).SingleOrDefaultAsync(cancellationToken);
            if (active == operation.NewCredentialSetId)
            {
                operation.MarkActive();
                continue;
            }

            if (operation.CreatesAccount && operation.AccountId is { } newAccountId)
            {
                if (active is not null)
                {
                    operation.MarkSuperseded();
                    continue;
                }
                if (!await _context.Accounts.AnyAsync(x => x.Id == newAccountId && x.UserId == operation.UserId && !x.IsDeleted, cancellationToken))
                {
                    operation.MarkCleaned();
                    continue;
                }
                var status = new SyncStatus(operation.UserId, newAccountId, "Bybit");
                status.ActivateCredentialSet(operation.NewCredentialSetId);
                _context.SyncStatuses.Add(status);
                operation.MarkActive();
                continue;
            }

            if (operation.PreviousCredentialVersion is not { } priorVersion)
            {
                operation.MarkCleaned();
                continue;
            }

            var newVersion = Guid.NewGuid();
            var activated = operation.AccountId is { } accountId
                ? await _context.SyncStatuses
                    .Where(x => x.UserId == operation.UserId && x.AccountId == accountId && x.ExchangeName == "Bybit" && x.ActiveCredentialSetId == operation.PreviousCredentialSetId && x.CredentialVersion == priorVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ActiveCredentialSetId, operation.NewCredentialSetId)
                        .SetProperty(x => x.CredentialVersion, newVersion), cancellationToken) == 1
                : await _context.ExchangeIntegrations
                    .Where(x => x.UserId == operation.UserId && x.Exchange == "Bybit" && x.ActiveCredentialSetId == operation.PreviousCredentialSetId && x.CredentialVersion == priorVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.ActiveCredentialSetId, operation.NewCredentialSetId)
                        .SetProperty(x => x.CredentialVersion, newVersion)
                        .SetProperty(x => x.Status, "Configured"), cancellationToken) == 1;
            if (activated) operation.MarkActive();
            else operation.MarkSuperseded();
        }
        await _context.SaveChangesAsync(cancellationToken);
        return operations.Count;
    }
    private async Task TrySaveAsync(CancellationToken cancellationToken) { try { await _context.SaveChangesAsync(cancellationToken); } catch (Exception ex) { _logger.LogError(ex, "Could not persist credential operation state"); } }
}
