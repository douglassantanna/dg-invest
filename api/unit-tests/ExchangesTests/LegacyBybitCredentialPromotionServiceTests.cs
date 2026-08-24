using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.ExchangesTests;

public class LegacyBybitCredentialPromotionServiceTests
{
    [Fact]
    public async Task PromoteAsync_DryRunDoesNotWriteAndExecutePromotesOnlyOnce()
    {
        var context = Context();
        var account = new Account("main", 7);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var vault = Vault((BybitCredentialKeys.LegacyAccountKey(7, account.Id, "api-key"), "key"), (BybitCredentialKeys.LegacyAccountKey(7, account.Id, "api-secret"), "secret"));
        var service = new LegacyBybitCredentialPromotionService(context, vault.Object, new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance));

        (await service.PromoteAsync(true, default)).Should().ContainSingle().Which.Outcome.Should().Be("Ready");
        context.LegacyBybitCredentialPromotions.Should().BeEmpty();
        vault.Verify(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        var promoted = (await service.PromoteAsync(false, default)).Single();
        promoted.Outcome.Should().Be("Promoted");
        (await context.ExchangeIntegrations.SingleAsync()).ActiveCredentialSetId.Should().Be(promoted.CredentialSetId);
        (await vault.Object.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(7, account.Id, "api-key"))).Value.Should().Be("key");
        (await service.PromoteAsync(false, default)).Single().CredentialSetId.Should().Be(promoted.CredentialSetId);
        context.LegacyBybitCredentialPromotions.Should().ContainSingle();
    }

    [Fact]
    public async Task PromoteAsync_ReportsIncompleteUnavailableAndPollutedWithoutMutation()
    {
        var context = Context();
        context.Accounts.AddRange(new Account("main", 1), new Account("main", 2), new Account("main", 3, EAccountType.Manual, "Bybit", "polluted"));
        await context.SaveChangesAsync();
        var vault = Vault();
        vault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(2, 2, "api-key"))).ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));
        var service = new LegacyBybitCredentialPromotionService(context, vault.Object, new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance));

        var results = await service.PromoteAsync(true, default);

        results.Select(x => x.Outcome).Should().BeEquivalentTo("Incomplete", "Unavailable", "PollutedManualAccount");
        context.ExchangeIntegrations.Should().BeEmpty();
        context.LegacyBybitCredentialPromotions.Should().BeEmpty();
        vault.Verify(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PromoteAsync_RequiresNonEmptyCredentialsAndRetriesUnavailableWithoutPromotion()
    {
        var context = Context();
        var incomplete = new Account("main", 1);
        var unavailable = new Account("main", 2);
        context.Accounts.AddRange(incomplete, unavailable);
        await context.SaveChangesAsync();
        var vault = Vault(
            (BybitCredentialKeys.LegacyAccountKey(1, incomplete.Id, "api-key"), ""),
            (BybitCredentialKeys.LegacyAccountKey(1, incomplete.Id, "api-secret"), "secret"),
            (BybitCredentialKeys.LegacyAccountKey(2, unavailable.Id, "api-key"), "key"),
            (BybitCredentialKeys.LegacyAccountKey(2, unavailable.Id, "api-secret"), "secret"));
        vault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(2, unavailable.Id, "webhook-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));
        var service = new LegacyBybitCredentialPromotionService(context, vault.Object, new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance));

        var first = await service.PromoteAsync(false, default);

        first.Should().BeEquivalentTo([
            new LegacyBybitCredentialPromotionReport(1, incomplete.Id, "Incomplete", "Incomplete", null, null),
            new LegacyBybitCredentialPromotionReport(2, unavailable.Id, "Unavailable", "Unavailable", null, null)]);
        context.ExchangeIntegrations.Should().BeEmpty();

        vault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(2, unavailable.Id, "webhook-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        (await service.PromoteAsync(false, default)).Single(x => x.UserId == 2).Outcome.Should().Be("Promoted");
    }

    [Fact]
    public async Task PromoteAsync_CopiesLegacyWebhookOrExplicitlyClearsItAndRejectsChangedSource()
    {
        var context = Context();
        var account = new Account("main", 7);
        var changedSource = new Account("main", 8);
        context.Accounts.AddRange(account, changedSource);
        await context.SaveChangesAsync();
        context.LegacyBybitCredentialPromotions.Add(new api.Exchanges.Models.LegacyBybitCredentialPromotion(8, 999));
        await context.SaveChangesAsync();
        var vault = Vault(
            (BybitCredentialKeys.LegacyAccountKey(7, account.Id, "api-key"), "key"),
            (BybitCredentialKeys.LegacyAccountKey(7, account.Id, "api-secret"), "secret"));
        var service = new LegacyBybitCredentialPromotionService(context, vault.Object, new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance));

        var reports = await service.PromoteAsync(false, default);

        var promoted = reports.Single(x => x.UserId == 7);
        (await vault.Object.GetSecretReadResultAsync(BybitCredentialKeys.SetKey(promoted.CredentialSetId!, "webhook-secret"))).Value.Should().BeEmpty();
        reports.Single(x => x.UserId == 8).Should().Be(new LegacyBybitCredentialPromotionReport(8, changedSource.Id, "SourceConflict", "Conflict", null, null));
    }

    private static DataContext Context() => new(new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<IKeyVaultService> Vault(params (string Key, string Value)[] entries)
    {
        var values = entries.ToDictionary(x => x.Key, x => x.Value);
        var vault = new Mock<IKeyVaultService>();
        vault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>())).ReturnsAsync((string key) => values.TryGetValue(key, out var value) ? new(KeyVaultSecretReadStatus.Found, value) : new(KeyVaultSecretReadStatus.NotFound));
        vault.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>())).Callback((string key, string value) => values[key] = value).Returns(Task.CompletedTask);
        return vault;
    }
}
