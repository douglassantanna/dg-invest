using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using api.AzureKeyVault;
using api.AzureStorage.Blob;
using api.Data;
using api.Exchanges.Bybit;
using api.Users.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace unit_tests.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ExchangeApiIntegrationCollection : ICollectionFixture<ExchangeApiIntegrationFixture>
{
    public const string Name = "exchange-api-integration";
}

public sealed class ExchangeApiIntegrationFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder()
        .WithPassword($"T{Guid.NewGuid():N}aA1!")
        .Build();

    public ExchangeApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        Factory = new ExchangeApiFactory(_database.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        await _database.DisposeAsync();
    }

    public async Task<(int UserId, int MainAccountId)> CreateUserAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User("Integration User", $"integration-{suffix}@example.com", "password", Role.User);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mainAccountId = await context.Accounts
            .Where(account => account.UserId == user.Id && account.Name == "main")
            .Select(account => account.Id)
            .SingleAsync();
        return (user.Id, mainAccountId);
    }
}

public sealed class ExchangeApiFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-secret-that-is-long-enough-for-hmac";
    private readonly string _connectionString;

    public InMemoryKeyVault KeyVault { get; } = new();
    public FakeBybitService Bybit { get; } = new();

    public ExchangeApiFactory(string connectionString) => _connectionString = connectionString;

    public HttpClient CreateAuthenticatedClient(int userId)
    {
        var client = CreateClient();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("JWTSettings:Secret", JwtSecret);
        builder.UseSetting("RateLimiterSettings:RequestsPermitLimit", "1000");
        builder.UseSetting("RateLimiterSettings:WindowLimitInMinutes", "1");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["JWTSettings:Secret"] = JwtSecret,
            ["RateLimiterSettings:RequestsPermitLimit"] = "1000",
            ["RateLimiterSettings:WindowLimitInMinutes"] = "1",
        }));
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DataContext>>();
            services.RemoveAll<DataContext>();
            services.AddDbContext<DataContext>(options => options.UseSqlServer(_connectionString));

            services.RemoveAll<IKeyVaultService>();
            services.RemoveAll<IBybitService>();
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IKeyVaultService>(KeyVault);
            services.AddSingleton<IBybitService>(Bybit);
            services.AddSingleton<IBlobStorageService, InMemoryBlobStorage>();
        });
    }
}

public sealed class InMemoryKeyVault : IKeyVaultService
{
    private readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal);
    public bool IsAvailable { get; set; } = true;
    public bool FailWrites { get; set; }

    public Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName)
    {
        if (!IsAvailable)
            return Task.FromResult(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));

        return Task.FromResult(_secrets.TryGetValue(secretName, out var value)
            ? new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, value)
            : new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        var result = await GetSecretReadResultAsync(secretName);
        if (result.IsUnavailable)
            throw new InvalidOperationException(KeyVaultSecretReadResult.UnavailableMessage);

        return result.Value;
    }

    public Task SetSecretAsync(string secretName, string value)
    {
        if (FailWrites)
            throw new InvalidOperationException("Key Vault write failed");

        _secrets[secretName] = value;
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string secretName)
    {
        _secrets.Remove(secretName);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryBlobStorage : IBlobStorageService
{
    public Task AppendLogAsync<T>(string containerName, string blobPath, T entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<List<T>> ReadLogsAsync<T>(string containerName, string blobPath, CancellationToken cancellationToken = default) => Task.FromResult(new List<T>());
}

public sealed class FakeBybitService : IBybitService
{
    public List<BybitSubMember> SubAccounts { get; } =
    [
        new BybitSubMember { Uid = "integration-uid-1", Username = "Integration", Remark = "Integration subaccount" },
    ];

    public bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret) => true;
    public Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret) => Task.FromResult(SubAccounts);
    public Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitOrderData>());
    public Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitDepositWithdrawalRow>());
    public Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitDepositWithdrawalRow>());
    public Task<bool> TestConnectionAsync(string apiKey, string apiSecret) => Task.FromResult(true);
}
