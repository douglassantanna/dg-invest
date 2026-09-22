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
    public static async Task<KeyVaultSecretReadResult> ReadAsync(IKeyVaultService vault, int userId, int? accountId, string suffix, CancellationToken cancellationToken = default, ILogger? logger = null)
    {
        var name = accountId is { } id
            ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
            : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);
        logger?.LogInformation("BybitCredentialReader resolved secret name {SecretName} for suffix {Suffix}", name, suffix);
        return await vault.GetSecretReadResultAsync(name);
    }
}

public interface IBybitCredentialSetService
{
    Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default);
    Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, string? webhookSecret, BybitRegion region, CancellationToken cancellationToken = default);
}

public record SaveCredentialResult(bool Success, bool Unavailable, string? Error = null);

public class BybitCredentialSetService : IBybitCredentialSetService
{
    private readonly DataContext _context;
    private readonly IKeyVaultService _vault;
    private readonly ILogger<BybitCredentialSetService> _logger;
    public BybitCredentialSetService(DataContext context, IKeyVaultService vault, ILogger<BybitCredentialSetService> logger)
        => (_context, _vault, _logger) = (context, vault, logger);

    public async Task<KeyVaultSecretReadResult> ReadAsync(int userId, int? accountId, string suffix, CancellationToken cancellationToken = default)
        => await BybitCredentialReader.ReadAsync(_vault, userId, accountId, suffix, cancellationToken);

    public async Task<SaveCredentialResult> SaveAsync(int userId, int? accountId, string apiKey, string apiSecret, string? webhookSecret, BybitRegion region, CancellationToken cancellationToken = default)
    {
        string Key(string suffix) => accountId is { } id
            ? BybitCredentialKeys.LegacyAccountKey(userId, id, suffix)
            : BybitCredentialKeys.LegacyIntegrationKey(userId, suffix);

        try
        {
            await _vault.SetSecretAsync(Key("api-key"), apiKey);
            await _vault.SetSecretAsync(Key("api-secret"), apiSecret);
            await _vault.SetSecretAsync(Key("webhook-secret"), webhookSecret ?? string.Empty);
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
