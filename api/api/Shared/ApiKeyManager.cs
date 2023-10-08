using System;
using api.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace api.Shared;
public class ApiKeyManager : IApiKeyManager
{
    public string GenerateApiKey()
    {
        string apiKey = Guid.NewGuid().ToString();
        return apiKey;
    }

    public string HashApiKey(string apiKey)
    {
        return BC.HashPassword(apiKey);
    }

    public bool VerifyApiKey(string apiKey, string hashedApiKey)
    {
        return BC.Verify(apiKey, hashedApiKey);
    }
}
