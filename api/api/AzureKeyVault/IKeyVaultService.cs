namespace api.AzureKeyVault;

public enum KeyVaultSecretReadStatus
{
    Found,
    NotFound,
    Unavailable
}

public record KeyVaultSecretReadResult(KeyVaultSecretReadStatus Status, string? Value = null)
{
    public const string UnavailableMessage = "Credential storage is temporarily unavailable. Please try again later.";

    public bool IsFound => Status == KeyVaultSecretReadStatus.Found;
    public bool IsUnavailable => Status == KeyVaultSecretReadStatus.Unavailable;
}

public interface IKeyVaultService
{
    Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName);
    Task<string?> GetSecretAsync(string secretName);
    Task SetSecretAsync(string secretName, string value);
    Task DeleteSecretAsync(string secretName);
}
