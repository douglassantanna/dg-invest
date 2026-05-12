namespace api.AzureKeyVault;
public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string secretName);
    Task SetSecretAsync(string secretName, string value);
    Task DeleteSecretAsync(string secretName);
}
