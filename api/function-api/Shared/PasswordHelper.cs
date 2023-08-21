using function_api.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace function_api.Shared;
public class PasswordHelper : IPasswordHelper
{
    public string EncryptPassword(string password)
    {
        return BC.HashPassword(password);
    }

    public bool VerifyPassword(string password, string encryptedPassword)
    {
        return BC.Verify(password, encryptedPassword);
    }
}



