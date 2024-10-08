using System.Text.Json.Serialization;
using api.Shared;
using api.Users.Models;

namespace api.Cryptos.Models;
public class Account : Entity
{
    public int UserId { get; private set; }
    public decimal Balance { get; private set; }
    private readonly List<AccountTransaction> _accountTransactions = new();
    public Account()
    {
    }

    public IReadOnlyCollection<AccountTransaction> AccountTransactions => _accountTransactions.AsReadOnly();
    internal void AddTransaction(AccountTransaction accountTransaction)
    {
        _accountTransactions.Add(accountTransaction);
    }
    internal void SubtractFromBalance(decimal balance)
    {
        Balance -= balance;
    }
    internal void AddToBalance(decimal balance)
    {
        Balance += balance;
    }
}
