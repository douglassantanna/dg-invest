using System.Text;
using api.Shared.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace api.Shared;
public class PasswordHelper : IPasswordHelper
{
    private readonly Random _random = new();
    public string EncryptPassword(string password)
    {
        return BC.HashPassword(password);
    }

    public bool VerifyPassword(string password, string encryptedPassword)
    {
        return BC.Verify(password, encryptedPassword);
    }
    public string RandomPassword()
    {
        var passwordBuilder = new StringBuilder();

        passwordBuilder.Append(RandomString(4, true));

        passwordBuilder.Append(RandomString(2));
        return passwordBuilder.ToString();
    }
    private string RandomString(int size, bool lowerCase = false)
    {
        var builder = new StringBuilder(size);

        char offset = lowerCase ? 'a' : 'A';
        const int lettersOffset = 26;

        for (var i = 0; i < size; i++)
        {
            var @char = (char)_random.Next(offset, offset + lettersOffset);
            builder.Append(@char);
        }

        return lowerCase ? builder.ToString().ToLower() : builder.ToString();
    }
}



