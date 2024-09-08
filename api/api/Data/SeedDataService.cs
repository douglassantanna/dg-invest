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
    private readonly string _baseUserEmail = "john8@email.com";
    private readonly string _baseCryptoSymbol = "BTC";
    private readonly string _baseUserPassword = "$2a$11$8TffsGWVArlU10uzS9LufuJNolCc15GiOgvaaMiiYpGqMPg0lRud.";
    private readonly string _baseUserName = "John Doe";
    private decimal _baseUserAccountInitialValue = 100000;
    private User? _user;
    private readonly ICryptoRepository _cryptoRepository;
    public SeedDataService(
        DataContext context,
        IUserRepository userRepository,
        ILogger<SeedDataService> logger,
        ICryptoRepository cryptoRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
        _cryptoRepository = cryptoRepository;
    }

    public async Task SeedDataAsync()
    {
        _logger.LogInformation("Starting data seeding process.");
        try
        {
            await MigrateDatabase();
            await SeedAdminUserIfNotExists();
            _user = await _userRepository.GetByIdAsync(2007, x => x.Include(x => x.CryptoAssets)
                                                                    .ThenInclude(c => c.Transactions)
                                                                .Include(x => x.Account));
            if (_user != null)
            {
                await SeedCryptosIfNotExists();
                await SeedCryptoAssetsIfNotExists();
                await SeedAccountIfNotExists();
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
            ];
            await _cryptoRepository.AddBatchAsync(cryptos);
        }
    }

    private async Task SeedCryptoAssetsIfNotExists()
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

    private async Task SeedAccountIfNotExists()
    {
        _baseUserAccountInitialValue = _baseUserAccountInitialValue - 20.72m - 4100.65m + 27.63m;
        _user.Account.AddToBalance(_baseUserAccountInitialValue);

        var accountTransactions = new List<AccountTransaction>
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

        foreach (var accountTransaction in accountTransactions)
        {
            _user.Account.AddTransaction(accountTransaction);
        }
    }
}