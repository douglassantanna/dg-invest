using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace api.AzureKeyVault;
public class KeyVaultService : IKeyVaultService
{
    private static readonly TimeSpan BybitSecretCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ReadFailureLogInterval = TimeSpan.FromMinutes(5);
    private const string BybitSecretCachePrefix = "bybit-keyvault-secret:";

    private readonly SecretClient _client;
    private readonly SecretClient _bybitClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyVaultService> _logger;

    public KeyVaultService(
        IOptions<KeyVaultSettings> settings,
        IOptions<BybitKeyVaultSettings> bybitSettings,
        IMemoryCache cache,
        ILogger<KeyVaultService> logger)
        : this(
            CreateClient(settings.Value.VaultUri),
            CreateClient(string.IsNullOrWhiteSpace(bybitSettings.Value.VaultUri)
                ? settings.Value.VaultUri
                : bybitSettings.Value.VaultUri),
            cache,
            logger)
    {
    }

    public KeyVaultService(SecretClient client, ILogger<KeyVaultService> logger)
        : this(client, client, new MemoryCache(new MemoryCacheOptions()), logger)
    {
    }

    public KeyVaultService(SecretClient client, SecretClient bybitClient, ILogger<KeyVaultService> logger)
        : this(client, bybitClient, new MemoryCache(new MemoryCacheOptions()), logger)
    {
    }

    public KeyVaultService(SecretClient client, SecretClient bybitClient, IMemoryCache cache, ILogger<KeyVaultService> logger)
    {
        _client = client;
        _bybitClient = bybitClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName)
    {
        var cacheKey = CacheKeyFor(secretName);
        if (cacheKey is not null && _cache.TryGetValue(cacheKey, out KeyVaultSecretReadResult? cached))
            return cached!;

        try
        {
            var response = await ClientFor(secretName).GetSecretAsync(secretName);
            var result = new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, response.Value.Value);
            _cache.Remove(FailureLogKeyFor(secretName));
            if (cacheKey is not null)
            {
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = BybitSecretCacheDuration
                });
            }

            return result;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404 && ex.ErrorCode == "SecretNotFound")
        {
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound);
        }
        catch (Azure.RequestFailedException ex)
        {
            LogReadFailure(secretName, ex);
            return new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable);
        }
        catch (Exception ex)
        {
            LogReadFailure(secretName, ex);
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
            InvalidateCachedSecret(secretName);
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
            InvalidateCachedSecret(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName} from Key Vault", secretName);
            throw;
        }
    }

    public void InvalidateCachedSecret(string secretName)
    {
        var cacheKey = CacheKeyFor(secretName);
        if (cacheKey is not null)
            _cache.Remove(cacheKey);
        _cache.Remove(FailureLogKeyFor(secretName));
    }

    private void LogReadFailure(string secretName, Exception exception)
    {
        var throttleKey = FailureLogKeyFor(secretName);
        if (_cache.TryGetValue(throttleKey, out _))
            return;

        _cache.Set(throttleKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ReadFailureLogInterval
        });
        _logger.LogError(exception, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
    }

    private static string? CacheKeyFor(string secretName)
        => secretName.StartsWith("bybit-", StringComparison.OrdinalIgnoreCase)
            ? $"{BybitSecretCachePrefix}{secretName}"
            : null;

    private static string FailureLogKeyFor(string secretName)
        => $"keyvault-read-failure-log:{secretName}";

    private SecretClient ClientFor(string secretName)
        => secretName.StartsWith("bybit-", StringComparison.OrdinalIgnoreCase)
            ? _bybitClient
            : _client;

    private static SecretClient CreateClient(string vaultUri)
        => new(new Uri(vaultUri), new DefaultAzureCredential());
}
