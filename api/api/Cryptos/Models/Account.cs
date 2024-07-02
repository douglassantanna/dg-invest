using api.Shared;
using api.Users.Models;

namespace api.Cryptos.Models;
public class Account : Entity
{
    public User User { get; private set; } = null!;
    public int UserId { get; private set; }
    public decimal Balance { get; private set; }
    private readonly List<AccountTransaction> _accountTransactions = new();
    public IReadOnlyCollection<AccountTransaction> AccountTransactions => _accountTransactions.AsReadOnly();
}
