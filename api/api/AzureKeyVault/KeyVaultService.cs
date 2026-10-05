using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;

namespace api.AzureKeyVault;
public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _client;
    private readonly SecretClient _bybitClient;
    private readonly ILogger<KeyVaultService> _logger;

    public KeyVaultService(
        IOptions<KeyVaultSettings> settings,
        IOptions<BybitKeyVaultSettings> bybitSettings,
        ILogger<KeyVaultService> logger)
        : this(
            CreateClient(settings.Value.VaultUri),
            CreateClient(string.IsNullOrWhiteSpace(bybitSettings.Value.VaultUri)
                ? settings.Value.VaultUri
                : bybitSettings.Value.VaultUri),
            logger)
    {
    }

    public KeyVaultService(SecretClient client, ILogger<KeyVaultService> logger)
        : this(client, client, logger)
    {
    }

    public KeyVaultService(SecretClient client, SecretClient bybitClient, ILogger<KeyVaultService> logger)
    {
        _client = client;
        _bybitClient = bybitClient;
        _logger = logger;
    }

    public async Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName)
    {
        try
        {
            var response = await ClientFor(secretName).GetSecretAsync(secretName);
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, response.Value.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404 && ex.ErrorCode == "SecretNotFound")
        {
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable);
        }
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        var result = await GetSecretReadResultAsync(secretName);
        if (result.IsUnavailable)
            throw new InvalidOperationException(KeyVaultSecretReadResult.UnavailableMessage);

        return result.Value;
    }

    public async Task SetSecretAsync(string secretName, string value)
    {
        try
        {
            await ClientFor(secretName).SetSecretAsync(secretName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName} in Key Vault", secretName);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string secretName)
    {
        try
        {
            await ClientFor(secretName).StartDeleteSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName} from Key Vault", secretName);
            throw;
        }
    }

    private SecretClient ClientFor(string secretName)
        => secretName.StartsWith("bybit-", StringComparison.OrdinalIgnoreCase)
            ? _bybitClient
            : _client;

    private static SecretClient CreateClient(string vaultUri)
        => new(new Uri(vaultUri), new DefaultAzureCredential());
}
