using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;

namespace api.AzureKeyVault;
public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _client;
    private readonly ILogger<KeyVaultService> _logger;

    public KeyVaultService(IOptions<KeyVaultSettings> settings, ILogger<KeyVaultService> logger)
    {
        _logger = logger;
        _client = new SecretClient(new Uri(settings.Value.VaultUri), new DefaultAzureCredential());
    }

    public KeyVaultService(SecretClient client, ILogger<KeyVaultService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName)
    {
        try
        {
            var response = await _client.GetSecretAsync(secretName);
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
            await _client.SetSecretAsync(secretName, value);
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
            await _client.StartDeleteSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName} from Key Vault", secretName);
            throw;
        }
    }
}
