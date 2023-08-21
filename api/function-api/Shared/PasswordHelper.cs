using System.Security.Cryptography;
using System.Text;
using function_api.Interfaces;

namespace function_api.Shared;
public class PasswordHelper : IPasswordHelper
{
    public string EncryptPassword(string password)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(password);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }

    public bool VerifyPassword(string password, string encryptedPassword)
    {
        string encryptedInput = EncryptPassword(password);
        return encryptedInput == encryptedPassword;
    }
}



