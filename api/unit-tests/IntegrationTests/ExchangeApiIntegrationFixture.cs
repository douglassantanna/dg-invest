using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using api.AzureKeyVault;
using api.AzureStorage.Blob;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Services;
using api.Users.Models;
using functions;
using Microsoft.Azure.Functions.Worker;
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

    public async Task<(int UserId, int MainAccountId)> CreateUserAsync(Role role = Role.User)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User("Integration User", $"integration-{suffix}@example.com", "password", role);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mainAccountId = await context.Accounts
            .Where(account => account.UserId == user.Id && account.Name == "main")
            .Select(account => account.Id)
            .SingleAsync();
        return (user.Id, mainAccountId);
    }

    public async Task RunBybitOrderSyncAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var function = new SyncBybitOrders(
            Factory.Bybit,
            scope.ServiceProvider.GetRequiredService<IBybitOrderSyncService>(),
            scope.ServiceProvider.GetRequiredService<IBybitAccountSyncService>(),
            Factory.KeyVault,
            scope.ServiceProvider.GetRequiredService<DataContext>(),
            scope.ServiceProvider.GetRequiredService<ILogger<SyncBybitOrders>>(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["BybitSync:Enabled"] = "true" })
                .Build());
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(context => context.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);
    }
}

public sealed class ExchangeApiFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-secret-that-is-long-enough-for-hmac";
    public const string MigrationToken = "integration-migration-token";
    private readonly string _connectionString;

    public InMemoryKeyVault KeyVault { get; } = new();
    public FakeBybitService Bybit { get; } = new();
    public FakeCoinMarketCapService CoinMarketCap { get; } = new();

    public ExchangeApiFactory(string connectionString) => _connectionString = connectionString;

    public HttpClient CreateAuthenticatedClient(int userId, Role role = Role.User)
    {
        var client = CreateClient();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
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
            ["Migrations:RemoteTriggerEnabled"] = "true",
            ["Migrations:RemoteTriggerToken"] = MigrationToken,
            ["BybitSync:Enabled"] = "true",
            ["RateLimiterSettings:RequestsPermitLimit"] = "1000",
            ["RateLimiterSettings:WindowLimitInMinutes"] = "1",
        }));
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DataContext>>();
            services.RemoveAll<DataContext>();
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(_connectionString, sql => sql.EnableRetryOnFailure()));

            services.RemoveAll<IKeyVaultService>();
            services.RemoveAll<IBybitService>();
            services.RemoveAll<ICoinMarketCapService>();
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IKeyVaultService>(KeyVault);
            services.AddSingleton<IBybitService>(Bybit);
            services.AddSingleton<ICoinMarketCapService>(CoinMarketCap);
            services.AddSingleton<IBlobStorageService, InMemoryBlobStorage>();
        });
    }
}

public sealed class FakeCoinMarketCapService : ICoinMarketCapService
{
    public Dictionary<string, Coin> CoinsBySymbol { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<GetQuoteResponse> GetQuoteBySymbol(string symbol)
    {
        var data = CoinsBySymbol.TryGetValue(symbol, out var coin)
            ? new Dictionary<string, Coin> { [symbol] = coin }
            : new Dictionary<string, Coin>();
        return Task.FromResult(new GetQuoteResponse(new Status(0, null), data));
    }

    public Task<GetQuoteResponse> GetQuotesByIds(string[] ids)
    {
        var data = CoinsBySymbol
            .Where(pair => ids.Contains(pair.Value.Id.ToString(), StringComparer.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(new GetQuoteResponse(new Status(0, null), data));
    }

    public decimal GetCryptoCurrencyPriceById(int coinMarketCapId, GetQuoteResponse cmpResponse) =>
        cmpResponse.Data.Values.FirstOrDefault(coin => coin.Id == coinMarketCapId)?.Quote.USD.Price ?? 0;
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
        if (!IsAvailable)
            throw new InvalidOperationException(KeyVaultSecretReadResult.UnavailableMessage);

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

    public string? LastApiKey { get; set; }
    public BybitRegion? LastRegion { get; set; }
    public BybitApiException? SubAccountsError { get; set; }
    public List<BybitOrderData> OrderHistory { get; } = [];
    public Dictionary<string, List<BybitExecutionData>> ExecutionsByOrderId { get; } = new(StringComparer.Ordinal);
    public BybitApiException? OrderHistoryError { get; set; }
    public string? OrderHistoryApiKey { get; set; }
    public long? LastOrderHistoryStartTime { get; private set; }
    public int OrderHistoryCallCount { get; private set; }
    public Dictionary<string, BybitWalletBalanceResponse> WalletBalancesByAccountType { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool ValidateWebhookSignature(string rawBody, string signature, string timestamp, string webhookSecret) => true;
    public Task<List<BybitSubMember>> GetSubAccountsAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global)
    {
        LastApiKey = apiKey;
        LastRegion = region;
        if (SubAccountsError is not null) throw SubAccountsError;
        return Task.FromResult(SubAccounts);
    }
    public Task<List<BybitOrderData>> GetOrderHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null)
    {
        LastApiKey = apiKey;
        LastRegion = region;
        LastOrderHistoryStartTime = startTime;
        OrderHistoryCallCount++;
        if (OrderHistoryError is not null)
            throw OrderHistoryError;
        if (OrderHistoryApiKey is not null && !string.Equals(OrderHistoryApiKey, apiKey, StringComparison.Ordinal))
            return Task.FromResult(new List<BybitOrderData>());
        return Task.FromResult(OrderHistory.Take(limit ?? OrderHistory.Count).ToList());
    }

    public Task<List<BybitExecutionData>> GetExecutionHistoryAsync(string apiKey, string apiSecret, BybitRegion region, string orderId) =>
        Task.FromResult(ExecutionsByOrderId.TryGetValue(orderId, out var executions)
            ? executions.ToList()
            : new List<BybitExecutionData>());
    public Task<List<BybitDepositWithdrawalRow>> GetDepositHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitDepositWithdrawalRow>());
    public Task<List<BybitDepositWithdrawalRow>> GetWithdrawalHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitDepositWithdrawalRow>());
    public Task<List<BybitInternalTransferRow>> GetInternalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitInternalTransferRow>());
    public Task<List<BybitInternalTransferRow>> GetUniversalTransferHistoryAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, int? limit = 50, long? startTime = null) => Task.FromResult(new List<BybitInternalTransferRow>());
    public Task<BybitWalletBalanceResponse> GetWalletBalanceAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global, string accountType = "UNIFIED", string? memberId = null)
    {
        LastApiKey = apiKey;
        LastRegion = region;
        return Task.FromResult(WalletBalancesByAccountType.TryGetValue(accountType, out var response)
            ? response
            : new BybitWalletBalanceResponse());
    }
    public Task<BybitAccountCoinBalanceResponse> GetAccountCoinBalanceAsync(string apiKey, string apiSecret, BybitRegion region, string accountType, string coin, string? memberId = null)
    {
        LastApiKey = apiKey;
        LastRegion = region;
        var wallet = WalletBalancesByAccountType.TryGetValue(accountType, out var response)
            ? response.Result.List.FirstOrDefault()?.Coin.FirstOrDefault(item => string.Equals(item.Coin, coin, StringComparison.OrdinalIgnoreCase))
            : null;
        return Task.FromResult(new BybitAccountCoinBalanceResponse
        {
            RetCode = 0,
            RetMsg = "success",
            Result = new BybitAccountCoinBalanceResult
            {
                AccountType = accountType,
                MemberId = memberId ?? string.Empty,
                Balance = new BybitAccountCoinBalance
                {
                    Coin = coin,
                    WalletBalance = wallet?.WalletBalance ?? "0",
                    TransferBalance = wallet?.AvailableBalance ?? wallet?.WalletBalance ?? "0"
                }
            }
        });
    }
    public Task<bool> TestConnectionAsync(string apiKey, string apiSecret, BybitRegion region = BybitRegion.Global) => Task.FromResult(true);
}
