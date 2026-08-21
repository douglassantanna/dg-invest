using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Cryptos.Models;
using api.Data;
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

        var saveMainCredentials = await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new
        {
            accountId = mainAccountId,
            apiKey = "main-api-key",
            apiSecret = "main-api-secret",
            webhookSecret = "main-webhook-secret",
        });
        saveMainCredentials.StatusCode.Should().Be(HttpStatusCode.OK);

        var syncAccounts = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
        syncAccounts.StatusCode.Should().Be(HttpStatusCode.OK);

        var exchangeAccountId = await GetAccountIdAsync(userId, "Integration subaccount");
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

        var manualAccountId = await GetAccountIdAsync(userId, "Manual portfolio");
        var map = await client.PostAsJsonAsync("/api/Exchange/bybit/map-account", new { accountId = manualAccountId, externalId = "integration-manual-uid" });
        map.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertAccountIsExchangeAsync(manualAccountId, "integration-manual-uid");

        var delete = await client.DeleteAsync($"/api/Exchange/bybit/credentials/{exchangeAccountId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
        var resync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
        resync.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertAccountIsDeletedAsync(exchangeAccountId);
    }

    private async Task<int> GetAccountIdAsync(int userId, string name)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DataContext>().Accounts
            .Where(account => account.UserId == userId && account.Name == name)
            .Select(account => account.Id)
            .SingleAsync();
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
