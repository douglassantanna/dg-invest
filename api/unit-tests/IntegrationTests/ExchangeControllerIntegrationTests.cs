using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using Microsoft.Extensions.DependencyInjection;

namespace unit_tests.IntegrationTests;

[Collection(ExchangeApiIntegrationCollection.Name)]
public class ExchangeControllerIntegrationTests
{
    private readonly ExchangeApiIntegrationFixture _fixture;

    public ExchangeControllerIntegrationTests(ExchangeApiIntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateAccount_WithNameProperty_ShouldPersistManualAccount()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/Account/create", new { name = "Name contract portfolio" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _fixture.Factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<DataContext>().Accounts.SingleAsync(
            candidate => candidate.UserId == userId && candidate.Name == "Name contract portfolio");
        account.AccountType.Should().Be(EAccountType.Manual);
        account.Exchange.Should().BeNull();
        account.ExternalId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAccount_WithoutNameOrLegacyAlias_ShouldRejectRequest()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/Account/create", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Account name is required");
    }

    [Fact]
    public async Task AccountAndBybitEndpoints_CompleteManagedSubaccountFlow()
    {
        var (userId, mainAccountId) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var createManual = await client.PostAsJsonAsync("/api/Account/create", new { subaccountTag = "Manual portfolio" });
        createManual.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await client.GetAsync("/api/Account");
        accounts.StatusCode.Should().Be(HttpStatusCode.OK);
        (await accounts.Content.ReadAsStringAsync()).Should().Contain("Manual portfolio");

        var saveIntegrationCredentials = await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new
        {
            apiKey = "integration-api-key",
            apiSecret = "integration-api-secret",
        });
        saveIntegrationCredentials.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertIntegrationCredentialsAsync(userId);

        var syncAccounts = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
        syncAccounts.StatusCode.Should().Be(HttpStatusCode.OK);

        var exchangeAccountId = await GetAccountIdAsync(userId, "Integration subaccount", EAccountType.Exchange);
        var saveSubaccountCredentials = await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new
        {
            accountId = exchangeAccountId,
            apiKey = "sub-api-key",
            apiSecret = "sub-api-secret",
            webhookSecret = "sub-webhook-secret",
        });
        saveSubaccountCredentials.StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.GetAsync("/api/Exchange/bybit/sub-members")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/Exchange/bybit/connection-groups")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/Exchange/bybit/credentials-status")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/Exchange/bybit/sync-status")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/Exchange/accounts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/Exchange/{exchangeAccountId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/Exchange/{exchangeAccountId}/transactions")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/Exchange/bybit/sync-logs/{exchangeAccountId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var testConnection = await client.PostAsync($"/api/Exchange/bybit/test-connection/{exchangeAccountId}", null);
        testConnection.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggle = await client.PostAsync($"/api/Exchange/bybit/toggle/{exchangeAccountId}", null);
        toggle.StatusCode.Should().Be(HttpStatusCode.OK);

        var delete = await client.DeleteAsync($"/api/Exchange/bybit/credentials/{exchangeAccountId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BybitDiscovery_PreservesSameNamedManualAccountAndMatchesOnlyUid()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        var createManual = await client.PostAsJsonAsync("/api/Account/create", new { name = "Integration subaccount" });
        createManual.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "key", apiSecret = "secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fixture.Factory.Services.CreateScope();
        var accounts = await scope.ServiceProvider.GetRequiredService<DataContext>().Accounts
            .Where(x => x.UserId == userId && x.Name == "Integration subaccount")
            .ToListAsync();
        accounts.Should().ContainSingle(x => x.AccountType == EAccountType.Manual && x.ExternalId == null && x.Exchange == null);
        accounts.Should().ContainSingle(x => x.AccountType == EAccountType.Exchange && x.Exchange == "Bybit" && x.ExternalId == "integration-uid-1");
    }

    [Fact]
    public async Task BybitDiscovery_RequiresIntegrationCredentials()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("integration credentials");
    }

    [Fact]
    public async Task BybitDiscovery_DistinguishesMissingCredentialsFromUnavailableKeyVault()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            context.ExchangeIntegrations.Add(new ExchangeIntegration(userId, "Bybit"));
            await context.SaveChangesAsync();
        }
        await _fixture.Factory.KeyVault.SetSecretAsync(
            SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(userId, "api-key"), "integration-api-key");
        await _fixture.Factory.KeyVault.SetSecretAsync(
            SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(userId, "api-secret"), "integration-api-secret");
        await _fixture.Factory.KeyVault.DeleteSecretAsync(
            SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(userId, "api-secret"));

        var missingSecret = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

        missingSecret.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingSecret.Content.ReadAsStringAsync()).Should().Contain("Bybit credentials not found");

        _fixture.Factory.KeyVault.IsAvailable = false;
        try
        {
            var unavailable = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

            unavailable.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await unavailable.Content.ReadAsStringAsync()).Should().Contain(KeyVaultSecretReadResult.UnavailableMessage);
        }
        finally
        {
            _fixture.Factory.KeyVault.IsAvailable = true;
        }
    }

    [Fact]
    public async Task BybitCredentialEndpoints_DistinguishUnavailableVaultFromMissingSecrets()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        var save = await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new
        {
            accountId = 0,
            name = "Credential status account",
            apiKey = "api-key",
            apiSecret = "api-secret",
            webhookSecret = "webhook-secret",
        });
        save.StatusCode.Should().Be(HttpStatusCode.OK);

        var accountId = await GetAccountIdAsync(userId, "Credential status account", EAccountType.Exchange);
        var endpoints = new[]
        {
            "/api/Exchange/bybit/connection-groups",
            "/api/Exchange/bybit/credentials-status",
            $"/api/Exchange/{accountId}",
        };

        _fixture.Factory.KeyVault.IsAvailable = false;
        try
        {
            foreach (var endpoint in endpoints)
                (await client.GetAsync(endpoint)).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            _fixture.Factory.KeyVault.IsAvailable = true;
        }

        await _fixture.Factory.KeyVault.DeleteSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, accountId, "api-key"));
        await _fixture.Factory.KeyVault.DeleteSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, accountId, "api-secret"));
        await _fixture.Factory.KeyVault.DeleteSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, accountId, "webhook-secret"));

        foreach (var endpoint in endpoints)
            (await client.GetAsync(endpoint)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await (await client.GetAsync("/api/Exchange/bybit/connection-groups")).Content.ReadAsStringAsync()).Should().Contain("pending");
        (await (await client.GetAsync("/api/Exchange/bybit/credentials-status")).Content.ReadAsStringAsync()).Should().Contain("\"hasApiKey\":false");
        (await (await client.GetAsync($"/api/Exchange/{accountId}")).Content.ReadAsStringAsync()).Should().Contain("\"hasApiKey\":false");
    }

    [Fact]
    public async Task BybitDiscovery_WithLegacyMainCredentials_ShouldExplainMigrationRequirement()
    {
        var (userId, mainAccountId) = await _fixture.CreateUserAsync();
        await _fixture.Factory.KeyVault.SetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, mainAccountId, "api-key"), "legacy-key");
        await _fixture.Factory.KeyVault.SetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, mainAccountId, "api-secret"), "legacy-secret");
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("need migration");
    }

    [Fact]
    public async Task ExchangeEndpoints_ShouldExcludeManualAccounts()
    {
        var (userId, mainAccountId) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var accounts = await client.GetAsync("/api/Exchange/accounts");
        accounts.StatusCode.Should().Be(HttpStatusCode.OK);
        (await accounts.Content.ReadAsStringAsync()).Should().NotContain("\"accountName\":\"main\"");

        (await client.GetAsync($"/api/Exchange/{mainAccountId}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var transactions = await client.GetAsync($"/api/Exchange/{mainAccountId}/transactions");
        transactions.StatusCode.Should().Be(HttpStatusCode.OK);
        (await transactions.Content.ReadAsStringAsync()).Should().Contain("Account not found");
    }

    [Fact]
    public async Task BybitAccountActions_RejectManualAccounts()
    {
        var (userId, mainAccountId) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        (await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new { accountId = mainAccountId, apiKey = "key", apiSecret = "secret", webhookSecret = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsync($"/api/Exchange/bybit/test-connection/{mainAccountId}", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsync($"/api/Exchange/bybit/toggle/{mainAccountId}", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/Exchange/bybit/map-account", new { accountId = mainAccountId, externalId = "manual-uid" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<int> GetAccountIdAsync(int userId, string name, EAccountType? accountType = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DataContext>().Accounts
            .Where(account => account.UserId == userId && account.Name == name && (accountType == null || account.AccountType == accountType))
            .Select(account => account.Id)
            .SingleAsync();
    }

    private async Task AssertIntegrationCredentialsAsync(int userId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        (await context.ExchangeIntegrations.CountAsync(x => x.UserId == userId && x.Exchange == "Bybit")).Should().Be(1);
        (await _fixture.Factory.KeyVault.GetSecretAsync($"bybit-integration-{userId}-api-key")).Should().Be("integration-api-key");
        (await _fixture.Factory.KeyVault.GetSecretAsync($"bybit-integration-{userId}-api-secret")).Should().Be("integration-api-secret");
    }

    private async Task AssertAccountIsExchangeAsync(int accountId, string externalId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<DataContext>().Accounts.SingleAsync(account => account.Id == accountId);
        account.AccountType.Should().Be(EAccountType.Exchange);
        account.Exchange.Should().Be("Bybit");
        account.ExternalId.Should().Be(externalId);
    }

    private async Task AssertAccountIsDeletedAsync(int accountId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var account = await context.Accounts.SingleAsync(account => account.Id == accountId);
        account.IsDeleted.Should().BeTrue();
        (await context.Accounts.CountAsync(candidate => candidate.ExternalId == account.ExternalId && !candidate.IsDeleted)).Should().Be(0);
    }
}
