using System.Reflection.Metadata;
using api.Cryptos.Models;
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
    private readonly string _userEmail = "john@email.com";
    public SeedDataService(
        DataContext context,
        IUserRepository userRepository,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SeedDataAsync()
    {
        _logger.LogInformation("Starting data seeding process.");
        try
        {
            await MigrateDatabase();
            await SeedAdminUserIfNotExists();
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
        var userExists = await _userRepository.IsUnique(_userEmail);

        if (!userExists)
        {
            _logger.LogInformation("Admin user does not exist, seeding admin user.");
            var user = new User("John Doe", _userEmail, "111111", Role.Admin, new Cryptos.Models.Account());
            try
            {
                await _userRepository.AddAsync(user);
                _logger.LogInformation("Admin user seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user creation.");
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
        List<Crypto> cryptos =
        [
            new Crypto()
        ];
    }
    /*private async Task SeedRegularUserIfNotExists()
    {
    }
    private async Task SeedCryptoAssetsIfNotExists()
    {
    }
    private async Task SeedAccountIfNotExists()
    {
    }*/
}