using api.Cryptos.Models;
using api.Cryptos.Repositories;
using api.Models.Cryptos;
using api.Users.Models;
using api.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Data;
public interface ISeedDataService
{
    Task SeedDataAsync();
}
public class SeedDataService : ISeedDataService
{
    private readonly DataContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SeedDataService> _logger;
    private string _baseUserEmail;
    private string _baseCryptoSymbol;
    private string _baseUserPassword;
    private string _baseUserName;
    private int _baseUserId;
    private User? _user;
    private readonly ICryptoRepository _cryptoRepository;
    public SeedDataService(
        DataContext context,
        IUserRepository userRepository,
        ILogger<SeedDataService> logger,
        ICryptoRepository cryptoRepository,
        IConfiguration configuration)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
        _cryptoRepository = cryptoRepository;
        _baseUserEmail = configuration["SeedDataSettings:BaseUserEmail"]!;
        _baseCryptoSymbol = configuration["SeedDataSettings:BaseCryptoSymbol"]!;
        _baseUserPassword = configuration["SeedDataSettings:BaseUserPassword"]!;
        _baseUserName = configuration["SeedDataSettings:BaseUserName"]!;
        _baseUserId = int.Parse(configuration["SeedDataSettings:BaseUserId"]!);
    }

    public async Task SeedDataAsync()
    {
        _logger.LogInformation("Starting data seeding process.");
        try
        {
            await MigrateDatabase();
            await SeedAdminUserIfNotExists();
            _user = await _userRepository.GetByIdAsync(_baseUserId, x => x.Include(x => x.CryptoAssets)
                                                                    .ThenInclude(c => c.Transactions)
                                                                .Include(x => x.Account));
            if (_user != null)
            {
                await SeedCryptosIfNotExists();
                SeedCryptoAssetsIfNotExists();
                SeedAccountIfNotExists();
                await _userRepository.UpdateAsync(_user);
            }
            _logger.LogInformation("Data seeding process completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the data seeding process.");
            throw;
        }
    }

    private async Task MigrateDatabase()
    {
        _logger.LogInformation("Starting database migration.");
        try
        {
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database migration.");
            throw;
        }
    }

    private async Task SeedAdminUserIfNotExists()
    {
        _logger.LogInformation("Checking if admin user exists.");
        var userExists = await _userRepository.IsUnique(_baseUserEmail);
        if (!userExists)
        {
            _logger.LogInformation("Admin user does not exist, seeding admin user.");
            var user = new User(_baseUserName, _baseUserEmail, _baseUserPassword, Role.Admin, new Account());
            try
            {
                await _userRepository.AddAsync(user);
                _logger.LogInformation("Admin user seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SeedAdminUserIfNotExists: An error occurred during user creation.");
                throw;
            }
        }
        else
        {
            _logger.LogInformation("Admin user already exists. No need to seed.");
        }
    }

    private async Task SeedCryptosIfNotExists()
    {
        var exists = await _cryptoRepository.ExistsAsync(_baseCryptoSymbol);
        if (!exists)
        {
            List<Crypto> cryptos =
            [
                new Crypto("Bitcoin","BTC","", 1),
                new Crypto("Chainlink", "LINK", "", 1975),
                new Crypto("Secret", "SCRT", "", 5604),
                new Crypto("Ethereum", "ETH", "", 1027),
                new Crypto("AAVE", "AAVE", "", 7278),
                new Crypto("Solana", "SOL", "", 5426)
            ]!;
            await _cryptoRepository.AddBatchAsync(cryptos);
        }
    }

    private void SeedCryptoAssetsIfNotExists()
    {
        var cryptoAsset = new CryptoAsset(cryptoCurrency: "BTC", currencyName: "USD", symbol: "BTCUSD", coinMarketCapId: 1);
        var cryptoTransactions = new List<CryptoTransaction>
        {
            new (amount: 0.00054m, price: 38366.98m, purchaseDate: DateTime.Now.AddDays(-21), exchangeName: "Bybit", transactionType: ETransactionType.Buy),
            new (amount: 0.08921m, price: 45966.21m, purchaseDate: DateTime.Now.AddDays(-17), exchangeName: "Bybit", transactionType: ETransactionType.Buy),
            new (amount: 0.00043m, price: 64247.77m, purchaseDate: DateTime.Now.AddDays(-11), exchangeName: "Bybit", transactionType: ETransactionType.Sell),
        };
        foreach (var cryptoTransaction in cryptoTransactions)
        {
            cryptoAsset.AddTransaction(cryptoTransaction);
        }

        var doesAssetExist = _user.CryptoAssets.Any(x => x.Symbol == cryptoAsset.Symbol);
        if (!doesAssetExist)
        {
            _user.AddCryptoAsset(cryptoAsset);
        }
    }

    private void SeedAccountIfNotExists()
    {
        if (_user.Account.Balance == 0)
        {
            decimal adjustedValue = CalculateInitialAccountValue();
            _user.Account.AddToBalance(adjustedValue);

            List<AccountTransaction> accountTransactions = InitializeAccountTransactions();
            AddTransactionsToAccount(accountTransactions);
        }
    }

    private static List<AccountTransaction> InitializeAccountTransactions()
    {
        return new List<AccountTransaction>
        {
            new (date: DateTime.Now.AddDays(-25),
                transactionType: EAccountTransactionType.DepositFiat,
                amount: 100000,
                notes: string.Empty),
            new (date: DateTime.Now.AddDays(-21),
                transactionType: EAccountTransactionType.Out,
                amount: 0.00054m,
                cryptoCurrentPrice: 38366.98m,
                exchangeName: "Bybit",
                notes: string.Empty,
                cryptoAssetId: 1,
                cryptoAsset: null),
            new (date: DateTime.Now.AddDays(-17),
                transactionType: EAccountTransactionType.Out,
                amount: 0.08921m,
                cryptoCurrentPrice: 45966.21m,
                exchangeName: "Bybit",
                notes: string.Empty,
                cryptoAssetId: 1,
                cryptoAsset: null),
            new (date: DateTime.Now.AddDays(-11),
                transactionType: EAccountTransactionType.In,
                amount: 0.00043m,
                cryptoCurrentPrice: 64247.77m,
                exchangeName: "Bybit",
                notes: string.Empty,
                cryptoAssetId: 1,
                cryptoAsset: null),
        };
    }

    private decimal CalculateInitialAccountValue()
    {
        return 100000 - 20.72m - 4100.65m + 27.63m;
    }

    private void AddTransactionsToAccount(List<AccountTransaction> accountTransactions)
    {
        foreach (var accountTransaction in accountTransactions)
        {
            _user.Account.AddTransaction(accountTransaction);
        }
    }
}