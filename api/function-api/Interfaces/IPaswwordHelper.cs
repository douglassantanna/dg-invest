namespace function_api.Interfaces;
public interface IPasswordHelper
{
    string EncryptPassword(string password);
    bool VerifyPassword(string password, string encryptedPassword);
}
