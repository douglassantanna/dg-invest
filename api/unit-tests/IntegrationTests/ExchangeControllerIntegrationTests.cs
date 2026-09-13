using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Users.Models;
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
        (await saveIntegrationCredentials.Content.ReadAsStringAsync()).Should().NotContain("integration-api-secret");

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
    public async Task BybitDiscovery_ShouldPopulateMainFundingAndSubaccountUnifiedCashBalances()
    {
        var originalSubAccounts = _fixture.Factory.Bybit.SubAccounts.ToList();
        try
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "sub-uid-1", Username = "Sub", Remark = "Trading subaccount" });
            _fixture.Factory.Bybit.WalletBalancesByAccountType.Clear();
            _fixture.Factory.Bybit.WalletBalancesByAccountType["FUND"] = WalletBalance("FUND", ("USDT", "3000"), ("USDC", "2000"), ("BTC", "1"));
            _fixture.Factory.Bybit.WalletBalancesByAccountType["UNIFIED"] = WalletBalance("UNIFIED", ("USDT", "6000"), ("USDC", "4000"), ("ETH", "2"));

            var (userId, mainAccountId) = await _fixture.CreateUserAsync();
            using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "integration-api-key", apiSecret = "integration-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            var syncAccounts = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

            syncAccounts.StatusCode.Should().Be(HttpStatusCode.OK);
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var main = await context.Accounts.SingleAsync(account => account.Id == mainAccountId);
            var sub = await context.Accounts.SingleAsync(account => account.UserId == userId && account.ExternalId == "sub-uid-1");
            main.Balance.Should().Be(15000m);
            sub.Balance.Should().Be(15000m);

            var openingTransactions = await context.AccountTransactions
                .Where(transaction => transaction.ExchangeTransactionId != null && transaction.ExchangeTransactionId.StartsWith("bybit-opening-balance-"))
                .ToListAsync();
            openingTransactions.Should().HaveCount(2);
            openingTransactions.Should().Contain(transaction => transaction.Amount == 15000m && transaction.TransactionType == EAccountTransactionType.DepositFiat);
            openingTransactions.Should().Contain(transaction => transaction.Amount == 15000m && transaction.TransactionType == EAccountTransactionType.DepositFiat);
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.AddRange(originalSubAccounts);
            _fixture.Factory.Bybit.WalletBalancesByAccountType.Clear();
        }
    }

    [Fact]
    public async Task DisconnectBybitIntegration_DisablesIntegrationAndSyncWithoutDeletingAccounts()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "integration-api-key", apiSecret = "integration-api-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var firstSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
        firstSync.StatusCode.Should().Be(HttpStatusCode.OK);
        (await firstSync.Content.ReadAsStringAsync()).Should().Contain("0 matched, 1 created");
        var secondSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
        secondSync.StatusCode.Should().Be(HttpStatusCode.OK);
        (await secondSync.Content.ReadAsStringAsync()).Should().Contain("1 matched, 0 created");
        var accountId = await GetAccountIdAsync(userId, "Integration subaccount", EAccountType.Exchange);
        var saveAccountCredentials = await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new
        {
            accountId,
            apiKey = "account-api-key",
            apiSecret = "account-api-secret",
            webhookSecret = "account-webhook-secret",
        });
        saveAccountCredentials.StatusCode.Should().Be(HttpStatusCode.OK);
        (await saveAccountCredentials.Content.ReadAsStringAsync()).Should().NotContain("account-api-secret");

        var disconnect = await client.PostAsync("/api/Exchange/bybit/disconnect", null);
        var repeatDisconnect = await client.PostAsync("/api/Exchange/bybit/disconnect", null);

        disconnect.StatusCode.Should().Be(HttpStatusCode.OK);
        repeatDisconnect.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var integration = await context.ExchangeIntegrations.SingleAsync(candidate => candidate.UserId == userId && candidate.Exchange == "Bybit");
        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be("Disconnected");
        var account = await context.Accounts.SingleAsync(candidate => candidate.Id == accountId);
        account.IsDeleted.Should().BeFalse();
        account.Enabled.Should().BeFalse();
        var status = await context.SyncStatuses.SingleAsync(candidate => candidate.UserId == userId && candidate.AccountId == accountId && candidate.ExchangeName == "Bybit");
        status.IsEnabled.Should().BeFalse();
        status.Status.Should().Be("Disconnected");
        var connectionGroupsAfterDisconnect = await client.GetAsync("/api/Exchange/bybit/connection-groups");
        connectionGroupsAfterDisconnect.StatusCode.Should().Be(HttpStatusCode.OK);
        var connectionGroupsPayload = await connectionGroupsAfterDisconnect.Content.ReadAsStringAsync();
        connectionGroupsPayload.Should().Contain("\"subaccountCount\":0");
        connectionGroupsPayload.Should().Contain("\"subaccounts\":[]");
        connectionGroupsPayload.Should().NotContain("Integration subaccount");
        (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key"))).Should().Be(string.Empty);
        (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyAccountKey(userId, accountId, "webhook-secret"))).Should().Be(string.Empty);
    }

    [Fact]
    public async Task BybitReconnect_WithDifferentCredentials_HidesStaleAccounts()
    {
        var originalSubAccounts = _fixture.Factory.Bybit.SubAccounts.ToList();
        try
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "first-uid", Username = "First", Remark = "First subaccount" });

            var (userId, _) = await _fixture.CreateUserAsync();
            using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "first-api-key", apiSecret = "first-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var firstSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
            firstSync.StatusCode.Should().Be(HttpStatusCode.OK);
            (await firstSync.Content.ReadAsStringAsync()).Should().Contain("1 created");

            var firstGroups = await client.GetAsync("/api/Exchange/bybit/connection-groups");
            var firstGroupsPayload = await firstGroups.Content.ReadAsStringAsync();
            firstGroupsPayload.Should().Contain("\"subaccountCount\":1");
            firstGroupsPayload.Should().Contain("First subaccount");

            var disconnect = await client.PostAsync("/api/Exchange/bybit/disconnect", null);
            disconnect.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var stale = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.ExternalId == "first-uid");
                stale.Enabled.Should().BeFalse();
                stale.IsDeleted.Should().BeFalse();
            }

            // Reconnect with a different Bybit API key that returns a different UID set.
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "second-uid", Username = "Second", Remark = "Second subaccount" });

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "second-api-key", apiSecret = "second-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var secondSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
            secondSync.StatusCode.Should().Be(HttpStatusCode.OK);
            (await secondSync.Content.ReadAsStringAsync()).Should().Contain("1 created");

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var stale = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.ExternalId == "first-uid");
                stale.Enabled.Should().BeFalse();
                stale.IsDeleted.Should().BeFalse();
                var fresh = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.ExternalId == "second-uid");
                fresh.Enabled.Should().BeTrue();
                fresh.IsDeleted.Should().BeFalse();
            }

            var reconnectGroups = await client.GetAsync("/api/Exchange/bybit/connection-groups");
            var reconnectPayload = await reconnectGroups.Content.ReadAsStringAsync();
            reconnectPayload.Should().Contain("\"subaccountCount\":1");
            reconnectPayload.Should().Contain("Second subaccount");
            reconnectPayload.Should().NotContain("First subaccount");

            var exchangeAccounts = await client.GetAsync("/api/Exchange/accounts");
            var exchangeAccountsPayload = await exchangeAccounts.Content.ReadAsStringAsync();
            exchangeAccountsPayload.Should().Contain("Second subaccount");
            exchangeAccountsPayload.Should().NotContain("First subaccount");
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.AddRange(originalSubAccounts);
        }
    }

    [Fact]
    public async Task BybitIntegrationCredentials_ReconnectUsesNewKey()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "first-api-key", apiSecret = "first-api-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var integration = await context.ExchangeIntegrations.SingleAsync(x => x.UserId == userId && x.Exchange == "Bybit");
            integration.Enabled.Should().BeTrue();
            (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key"))).Should().Be("first-api-key");
        }

        _fixture.Factory.Bybit.LastApiKey = null;
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        _fixture.Factory.Bybit.LastApiKey.Should().Be("first-api-key");

        (await client.PostAsync("/api/Exchange/bybit/disconnect", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "second-api-key", apiSecret = "second-api-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var integration = await context.ExchangeIntegrations.SingleAsync(x => x.UserId == userId && x.Exchange == "Bybit");
            (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key"))).Should().Be("second-api-key");
            (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-secret"))).Should().Be("second-api-secret");
        }

        _fixture.Factory.Bybit.LastApiKey = null;
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        _fixture.Factory.Bybit.LastApiKey.Should().Be("second-api-key");
    }

    [Fact]
    public async Task BybitIntegrationCredentials_WithEuRegion_UsesEuHostForSync()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials",
            new { apiKey = "eu-api-key", apiSecret = "eu-api-secret", Region = "Eu" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Factory.Bybit.LastRegion = null;
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        _fixture.Factory.Bybit.LastRegion.Should().Be(BybitRegion.Eu);
    }

    [Fact]
    public async Task BybitIntegrationCredentials_ResaveWithoutDisconnect_UsesNewKey()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "first-api-key", apiSecret = "first-api-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Factory.Bybit.LastApiKey = null;
        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "second-api-key", apiSecret = "second-api-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Factory.Bybit.LastApiKey = null;
        (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        _fixture.Factory.Bybit.LastApiKey.Should().Be("second-api-key");
    }

    [Fact]
    public async Task BybitReconnect_FromMultiSubaccountToDifferentAccount_HidesAllStaleAccounts()
    {
        var originalSubAccounts = _fixture.Factory.Bybit.SubAccounts.ToList();
        try
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.AddRange(new[]
            {
                new BybitSubMember { Uid = "old-main", Username = "Old", Remark = "Old main account" },
                new BybitSubMember { Uid = "old-sub-1", Username = "OldSub1", Remark = "Old sub 1" },
                new BybitSubMember { Uid = "old-sub-2", Username = "OldSub2", Remark = "Old sub 2" },
            });

            var (userId, _) = await _fixture.CreateUserAsync();
            using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "old-api-key", apiSecret = "old-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                (await context.Accounts.CountAsync(a => a.UserId == userId && a.Exchange == "Bybit" && !a.IsDeleted)).Should().Be(3);
            }

            (await client.PostAsync("/api/Exchange/bybit/disconnect", null)).StatusCode.Should().Be(HttpStatusCode.OK);

            // Brand-new Bybit account (main only, different UIDs).
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "new-main", Username = "New", Remark = "New main account" });

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "new-api-key", apiSecret = "new-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsync("/api/Exchange/bybit/sync-accounts", null)).StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var stale = await context.Accounts
                    .Where(a => a.UserId == userId && a.Exchange == "Bybit" && !a.IsDeleted && a.ExternalId!.StartsWith("old-"))
                    .ToListAsync();
                stale.Should().HaveCount(3);
                stale.Should().OnlyContain(a => a.Enabled == false);
                var fresh = await context.Accounts.SingleAsync(a => a.UserId == userId && a.ExternalId == "new-main");
                fresh.Enabled.Should().BeTrue();
            }

            var groups = await client.GetAsync("/api/Exchange/bybit/connection-groups");
            var payload = await groups.Content.ReadAsStringAsync();
            payload.Should().Contain("\"subaccountCount\":1");
            payload.Should().Contain("New main account");
            payload.Should().NotContain("Old main account");
            payload.Should().NotContain("Old sub 1");
            payload.Should().NotContain("Old sub 2");
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.AddRange(originalSubAccounts);
        }
    }

    [Fact]
    public async Task BybitSync_DisablesStaleAccountsWhenApiKeyChanges()
    {
        var originalSubAccounts = _fixture.Factory.Bybit.SubAccounts.ToList();
        try
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "stale-uid", Username = "Stale", Remark = "Stale subaccount" });

            var (userId, _) = await _fixture.CreateUserAsync();
            using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "stale-api-key", apiSecret = "stale-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var firstSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
            firstSync.StatusCode.Should().Be(HttpStatusCode.OK);
            (await firstSync.Content.ReadAsStringAsync()).Should().Contain("1 created");

            // Swap integration credentials to a different Bybit account without disconnecting.
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.Add(new BybitSubMember { Uid = "fresh-uid", Username = "Fresh", Remark = "Fresh subaccount" });

            (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "fresh-api-key", apiSecret = "fresh-api-secret" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var secondSync = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);
            secondSync.StatusCode.Should().Be(HttpStatusCode.OK);
            var secondSyncPayload = await secondSync.Content.ReadAsStringAsync();
            secondSyncPayload.Should().Contain("1 created");
            secondSyncPayload.Should().Contain("1 disabled");

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var stale = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.ExternalId == "stale-uid");
                stale.Enabled.Should().BeFalse();
                stale.IsDeleted.Should().BeFalse();
                var fresh = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.ExternalId == "fresh-uid");
                fresh.Enabled.Should().BeTrue();
            }

            var groups = await client.GetAsync("/api/Exchange/bybit/connection-groups");
            var groupsPayload = await groups.Content.ReadAsStringAsync();
            groupsPayload.Should().Contain("\"subaccountCount\":1");
            groupsPayload.Should().Contain("Fresh subaccount");
            groupsPayload.Should().NotContain("Stale subaccount");
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccounts.Clear();
            _fixture.Factory.Bybit.SubAccounts.AddRange(originalSubAccounts);
        }
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
    public async Task BybitDiscovery_WhenBybitRejectsCredentials_ShouldReturnErrorWithoutCreatingAccounts()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "invalid-key", apiSecret = "invalid-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Factory.Bybit.SubAccountsError = new BybitApiException(10003, "API key is invalid");
        try
        {
            var response = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var payload = await response.Content.ReadAsStringAsync();
            payload.Should().Contain("Bybit rejected the integration credentials: 10003 - API key is invalid");
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            (await context.Accounts.CountAsync(account => account.UserId == userId
                && account.AccountType == EAccountType.Exchange
                && account.Exchange == "Bybit")).Should().Be(0);
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccountsError = null;
        }
    }

    [Fact]
    public async Task BybitSubMembers_WhenBybitRejectsCredentials_ShouldReturnBybitError()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        (await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new { apiKey = "invalid-key", apiSecret = "invalid-secret" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Factory.Bybit.SubAccountsError = new BybitApiException(10003, "API key is invalid");
        try
        {
            var response = await client.GetAsync("/api/Exchange/bybit/sub-members");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should()
                .Contain("Bybit rejected the integration credentials: 10003 - API key is invalid");
        }
        finally
        {
            _fixture.Factory.Bybit.SubAccountsError = null;
        }
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
    public async Task BybitCredentialEndpoints_AcceptLegacyAccountAliasesAndReturnSuccess()
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var integration = await client.PostAsJsonAsync("/api/Exchange/bybit/integration-credentials", new
        {
            apiKey = "integration-api-key",
            apiSecret = "integration-api-secret",
        });
        var account = await client.PostAsJsonAsync("/api/Exchange/bybit/credentials", new
        {
            accountId = 0,
            subaccountTag = "Legacy aliases account",
            bybitUid = "legacy-uid-001",
            apiKey = "account-api-key",
            apiSecret = "account-api-secret",
            webhookSecret = "account-webhook-secret",
        });

        integration.StatusCode.Should().Be(HttpStatusCode.OK);
        account.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var exchangeAccount = await context.Accounts.SingleAsync(candidate => candidate.UserId == userId && candidate.Name == "Legacy aliases account");
        exchangeAccount.ExternalId.Should().Be("legacy-uid-001");
        (await context.ExchangeIntegrations.SingleAsync(candidate => candidate.UserId == userId && candidate.Exchange == "Bybit")).Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/Exchange/bybit/integration-credentials")]
    [InlineData("/api/Exchange/bybit/credentials")]
    public async Task BybitCredentialEndpoints_Return503AndRecordRecoveryWhenKeyVaultIsUnavailable(string endpoint)
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        _fixture.Factory.KeyVault.IsAvailable = false;
        try
        {
            var response = endpoint.EndsWith("integration-credentials", StringComparison.Ordinal)
                ? await client.PostAsJsonAsync(endpoint, new { apiKey = "api-key", apiSecret = "api-secret" })
                : await client.PostAsJsonAsync(endpoint, new { accountId = 0, subaccountTag = "Unavailable account", bybitUid = "unavailable-uid", apiKey = "api-key", apiSecret = "api-secret", webhookSecret = "" });

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var payload = await response.Content.ReadAsStringAsync();
            payload.Should().Contain(KeyVaultSecretReadResult.UnavailableMessage);
            payload.Should().Contain("\"data\":503");
        }
        finally
        {
            _fixture.Factory.KeyVault.IsAvailable = true;
        }
    }

    [Theory]
    [InlineData("/api/Exchange/bybit/integration-credentials")]
    [InlineData("/api/Exchange/bybit/credentials")]
    public async Task BybitCredentialEndpoints_ReturnErrorAndLeavePendingStatusWhenVaultWriteFails(string endpoint)
    {
        var (userId, _) = await _fixture.CreateUserAsync();
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);
        _fixture.Factory.KeyVault.FailWrites = true;
        try
        {
            var response = endpoint.EndsWith("integration-credentials", StringComparison.Ordinal)
                ? await client.PostAsJsonAsync(endpoint, new { apiKey = "api-key", apiSecret = "api-secret" })
                : await client.PostAsJsonAsync(endpoint, new { accountId = 0, subaccountTag = "Pending account", bybitUid = "pending-uid", apiKey = "api-key", apiSecret = "api-secret", webhookSecret = "webhook-secret" });

            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            body.Should().Contain("recovery may be required");
        }
        finally
        {
            _fixture.Factory.KeyVault.FailWrites = false;
        }

        if (!endpoint.EndsWith("integration-credentials", StringComparison.Ordinal))
        {
            var groups = await client.GetAsync("/api/Exchange/bybit/connection-groups");
            groups.StatusCode.Should().Be(HttpStatusCode.OK);
            (await groups.Content.ReadAsStringAsync()).Should().Contain("\"status\":\"pending\"");
        }
    }

    [Fact]
    public async Task BybitDiscovery_WithOnlyMainAccountCredentials_ShouldRequireIntegrationCredentials()
    {
        var (userId, mainAccountId) = await _fixture.CreateUserAsync();
        await _fixture.Factory.KeyVault.SetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, mainAccountId, "api-key"), "legacy-key");
        await _fixture.Factory.KeyVault.SetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, mainAccountId, "api-secret"), "legacy-secret");
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsync("/api/Exchange/bybit/sync-accounts", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("integration credentials");
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

    [Fact]
    public async Task RunMigrations_RequiresAdmin()
    {
        var (userId, _) = await _fixture.CreateUserAsync();

        (await _fixture.Factory.CreateClient().PostAsync("/api/Migrations/run", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var user = _fixture.Factory.CreateAuthenticatedClient(userId);
        (await user.PostAsync("/api/Migrations/run", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var admin = _fixture.Factory.CreateAuthenticatedClient(userId, Role.Admin);
        (await admin.PostAsync("/api/Migrations/run", null)).StatusCode.Should().Be(HttpStatusCode.OK);
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
        var integration = await context.ExchangeIntegrations.SingleAsync(x => x.UserId == userId && x.Exchange == "Bybit");
        integration.Enabled.Should().BeTrue();
        (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key"))).Should().Be("integration-api-key");
        (await _fixture.Factory.KeyVault.GetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-secret"))).Should().Be("integration-api-secret");
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

    private static BybitWalletBalanceResponse WalletBalance(string accountType, params (string Coin, string Balance)[] coins) => new()
    {
        RetCode = 0,
        RetMsg = "OK",
        Result = new BybitWalletBalanceResult
        {
            List =
            [
                new BybitWalletBalanceAccount
                {
                    AccountType = accountType,
                    Coin = coins.Select(coin => new BybitWalletBalanceCoin
                    {
                        Coin = coin.Coin,
                        WalletBalance = coin.Balance,
                        AvailableBalance = coin.Balance,
                        UsdValue = coin.Balance
                    }).ToList()
                }
            ]
        }
    };
}
