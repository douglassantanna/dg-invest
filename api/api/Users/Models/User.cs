using System.Text.Json.Serialization;
using api.Cryptos.Models;
using api.Shared;

namespace api.Users.Models;
public class User : Entity
{
    private const string MainAccount = "main";
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool EmailConfirmed { get; set; } = false;

    public DateTime CreatedAt { get; private set; }
    private List<Account> _accounts = new();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();
    public User(string fullName,
                string email,
                string password,
                Role role)
    {
        FullName = StringSanitizer.Sanitize(fullName);
        Email = email;
        Password = password;
        Role = role;
        CreatedAt = DateTime.Now;
        AddAccount(MainAccount);
    }

    protected User() { }

    internal void SelectAccount(string subAccountTag)
    {
        foreach (var account in _accounts)
        {
            if (account.SubaccountTag == subAccountTag)
            {
                account.Select();
            }
            account.Deselect();
        }
    }
    internal Response AddAccount(string subaccountTag)
    {
        var accountExists = Accounts.Any(x => x.SubaccountTag.Equals(subaccountTag, StringComparison.OrdinalIgnoreCase));
        if (accountExists)
            return new Response($"Subaccount tag '{subaccountTag}' already exists for this user.", false);

        _accounts.Add(new Account(subaccountTag, Id));
        return new Response("", true);
    }

    internal Account? GetAccountByTag(string subaccountTag)
        => Accounts.FirstOrDefault(x => x.SubaccountTag.Equals(subaccountTag, StringComparison.OrdinalIgnoreCase));


    internal void Update(string fullname, string email)
    {
        FullName = StringSanitizer.Sanitize(fullname);
        Email = email;
    }
    internal void UpdatePassword(string newEncryptedPassword)
    {
        if (string.IsNullOrEmpty(newEncryptedPassword))
            throw new ArgumentNullException(nameof(newEncryptedPassword), "New encrypted password cannot be null or empty.");

        Password = newEncryptedPassword;
    }
}