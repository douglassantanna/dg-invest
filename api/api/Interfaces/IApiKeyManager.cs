namespace api.Interfaces;
public interface IApiKeyManager
{
    string GenerateApiKey();
    string HashApiKey(string apiKey);
    bool VerifyApiKey(string apiKey, string hashedApiKey);
}
