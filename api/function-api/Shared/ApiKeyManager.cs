using System;
using function_api.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace function_api.Shared;
public class ApiKeyManager : IApiKeyManager
{
    public string GenerateApiKey()
    {
        var apiKey = Guid.NewGuid();
        return apiKey.ToString();
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
