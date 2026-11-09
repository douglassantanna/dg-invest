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
    private static readonly string[] CacheableCredentialSuffixes = ["api-key", "api-secret", "webhook-secret"];

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
    Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, string? webhookSecret, BybitRegion region, CancellationToken cancellationToken = default);
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

    // DG Invest only reads from Bybit. A key that can trade (or withdraw, on a master account) is refused before it is
    // stored, which also verifies the key against Bybit instead of saving an invalid one.
    private async Task<SaveCredentialResult?> VerifyReadOnlyKeyAsync(string apiKey, string apiSecret, BybitRegion region, CancellationToken cancellationToken)
    {
        try
        {
            var info = await _bybit.GetApiKeyInfoAsync(apiKey, apiSecret, region, cancellationToken);
            return info.IsReadOnly
                ? null
                : new SaveCredentialResult(false, false,
                    "This Bybit API key can trade or withdraw. Create a read-only key in Bybit (API Management > Read-Only) and try again. "
                    + "For the master account key, also enable Wallet > Account Transfer / Subaccount Transfer so sub-accounts can be discovered.",
                    400);
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

    public async Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, string? webhookSecret, BybitRegion region, CancellationToken cancellationToken = default)
    {
        string Key(string suffix) => accountId is { } id
            ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
            : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);

        if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret)
            && await VerifyReadOnlyKeyAsync(apiKey, apiSecret, region, cancellationToken) is { } rejection)
            return rejection;

        try
        {
            // Write only the secrets the caller provided. A partial update (e.g. only a webhook secret,
            // or a key rotation without re-typing the webhook secret) must not blank the stored values.
            if (!string.IsNullOrEmpty(apiKey))
                await _vault.SetSecretAsync(Key("api-key"), apiKey);
            if (!string.IsNullOrEmpty(apiSecret))
                await _vault.SetSecretAsync(Key("api-secret"), apiSecret);
            if (!string.IsNullOrEmpty(webhookSecret))
                await _vault.SetSecretAsync(Key("webhook-secret"), webhookSecret);
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
