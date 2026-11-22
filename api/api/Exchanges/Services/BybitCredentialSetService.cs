using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Exchanges.Services;

public static class BybitCredentialKeys
{
    public static string LegacyAccountKey(int userId, int accountId, string suffix) => $"bybit-{userId}-{accountId}-{suffix}";
    public static string LegacyIntegrationKey(int userId, string suffix) => $"bybit-integration-{userId}-{suffix}";
}

public static class BybitCredentialReader
{
    private static readonly string[] CacheableCredentialSuffixes = ["api-key", "api-secret"];

    public static async Task<KeyVaultSecretReadResult> ReadAsync(IKeyVaultService vault, int userId, int? accountId, string suffix, CancellationToken cancellationToken = default, ILogger? logger = null)
    {
        var name = accountId is { } id
            ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
            : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);
        logger?.LogInformation("BybitCredentialReader resolved secret name {SecretName} for suffix {Suffix}", name, suffix);
        return await vault.GetSecretReadResultAsync(name);
    }

    public static void Invalidate(IKeyVaultService vault, int userId, int? accountId)
    {
        foreach (var suffix in CacheableCredentialSuffixes)
        {
            var name = accountId is { } id
                ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
                : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);
            vault.InvalidateCachedSecret(name);
        }
    }
}

public interface IBybitCredentialSetService
{
    Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default);
    // expectedOwnerUid: the Bybit UID the key must belong to. When null and accountId is set, the account's UID is used.
    Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, BybitRegion region, CancellationToken cancellationToken = default, string? expectedOwnerUid = null);
}

// RejectedStatusCode is set when the credentials themselves were refused (e.g. not a read-only key); Error then holds
// a user-facing message and nothing was written.
public record SaveCredentialResult(bool Success, bool Unavailable, string? Error = null, int? RejectedStatusCode = null);

public class BybitCredentialSetService : IBybitCredentialSetService
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _vault;
    private readonly IBybitService _bybit;
    private readonly ILogger<BybitCredentialSetService> _logger;
    public BybitCredentialSetService(DataContext context, IKeyVaultService vault, IBybitService bybit, ILogger<BybitCredentialSetService> logger)
        => (_context, _vault, _bybit, _logger) = (context, vault, bybit, logger);

    public async Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default)
        => await BybitCredentialReader.ReadAsync(_vault, userId, accountId, suffix, cancellationToken);

    // DG Invest only reads from Bybit. Before a key is stored it is checked with Bybit: it must be valid, read-only
    // (cannot trade or withdraw), and belong to the expected UID. A key from another sub-account would make this account
    // mirror that other wallet (STAGE account 34 had rentAccount's key).
    private async Task<SaveCredentialResult?> VerifyKeyAsync(string apiKey, string apiSecret, BybitRegion region, string? expectedOwnerUid, CancellationToken cancellationToken)
    {
        try
        {
            var info = await _bybit.GetApiKeyInfoAsync(apiKey, apiSecret, region, cancellationToken);
            if (!info.IsReadOnly)
                return new SaveCredentialResult(false, false,
                    "This Bybit API key can trade or withdraw. Create a read-only key in Bybit (API Management > Read-Only) and try again. "
                    + "For the master account key, also enable Wallet > Account Transfer / Subaccount Transfer so sub-accounts can be discovered.",
                    400);

            // UserId 0 means Bybit did not report the owner; the check is skipped rather than blocking the save.
            var ownerUid = info.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (info.UserId != 0 && !string.IsNullOrWhiteSpace(expectedOwnerUid)
                && !string.Equals(ownerUid, expectedOwnerUid.Trim(), StringComparison.Ordinal))
                return new SaveCredentialResult(false, false,
                    $"This Bybit API key belongs to UID {ownerUid}, but this account is UID {expectedOwnerUid.Trim()}. "
                    + "Create a read-only key inside that Bybit account and try again.",
                    400);

            return null;
        }
        catch (BybitApiException ex)
        {
            _logger.LogWarning("Bybit rejected the API key during credential save: {RetCode} {RetMsg}", ex.RetCode, ex.RetMsg);
            return new SaveCredentialResult(false, false,
                $"Bybit rejected this API key ({ex.RetCode}). Check the key, secret and region, then try again.",
                400);
        }
        catch (BybitTransportException ex)
        {
            _logger.LogWarning(ex, "Bybit was unreachable while verifying an API key");
            return new SaveCredentialResult(false, false,
                "Could not reach Bybit to verify the API key. Try again in a moment.",
                503);
        }
    }

    public async Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, BybitRegion region, CancellationToken cancellationToken = default, string? expectedOwnerUid = null)
    {
        string Key(string suffix) => accountId is { } id
            ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
            : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);

        if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            expectedOwnerUid ??= accountId is { } accountIdForUid
                ? await _context.Accounts.AsNoTracking()
                    .Where(account => account.Id == accountIdForUid && account.UserId == userId)
                    .Select(account => account.ExternalId)
                    .SingleOrDefaultAsync(cancellationToken)
                : null;
            if (await VerifyKeyAsync(apiKey, apiSecret, region, expectedOwnerUid, cancellationToken) is { } rejection)
                return rejection;
        }

        try
        {
            // Write only the secrets the caller provided, so a partial call never blanks a stored value.
            if (!string.IsNullOrEmpty(apiKey))
                await _vault.SetSecretAsync(Key("api-key"), apiKey);
            if (!string.IsNullOrEmpty(apiSecret))
                await _vault.SetSecretAsync(Key("api-secret"), apiSecret);
        }
        catch (Exception ex)
        {
            if (string.Equals(ex.Message, KeyVaultSecretReadResult.UnavailableMessage, StringComparison.Ordinal))
                return new SaveCredentialResult(false, true, KeyVaultSecretReadResult.UnavailableMessage);

            _logger.LogError(ex, "Bybit credential save failed to write Key Vault for user {UserId}, account {AccountId}", userId, accountId);
            return new SaveCredentialResult(false, false, "Failed to save credentials");
        }

        try
        {
            if (accountId is { } id)
            {
                var status = await _context.SyncStatuses
                    .SingleOrDefaultAsync(x => x.UserId == userId && x.AccountId == id && x.ExchangeName == "Bybit", cancellationToken);
                if (status is null)
                {
                    status = new SyncStatus(userId, id, "Bybit");
                    _context.SyncStatuses.Add(status);
                }
                status.SetRegion(region.ToString());
                status.EnableForCredentials();
            }
            else
            {
                var integration = await _context.ExchangeIntegrations
                    .SingleOrDefaultAsync(x => x.UserId == userId && x.Exchange == "Bybit", cancellationToken);
                if (integration is null)
                {
                    integration = new ExchangeIntegration(userId, "Bybit");
                    _context.ExchangeIntegrations.Add(integration);
                }
                integration.SetRegion(region.ToString());
                integration.MarkConfigured();
                integration.MarkEnabled();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new SaveCredentialResult(true, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bybit credential save failed to persist owner state for user {UserId}, account {AccountId}", userId, accountId);
            return new SaveCredentialResult(false, false, "Failed to save credentials");
        }
    }
}
