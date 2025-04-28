namespace api.Shared.Interfaces;
public interface IPasswordHelper
{
    string EncryptPassword(string password);
    string RandomPassword();
    bool VerifyPassword(string password, string encryptedPassword);
}
