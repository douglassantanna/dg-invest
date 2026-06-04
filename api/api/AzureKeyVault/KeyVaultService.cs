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

    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            var response = await _client.GetSecretAsync(secretName);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return null;
        }
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
