using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace unit_tests.ExchangesTests.Commands;

public class CredentialSagaSqlServerConcurrencyTests
{
    [Fact]
    public async Task ReplaceAsync_WhenSyncStatusActivationRaces_ShouldKeepExactlyOneWinner()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using (var setupContext = new DataContext(options))
        {
            await setupContext.Database.MigrateAsync();
            var status = new SyncStatus(1, 1, "Bybit");
            status.ActivateCredentialSet("original-set");
            setupContext.SyncStatuses.Add(status);
            await setupContext.SaveChangesAsync();
        }

        using var vault = new ActivationBarrierKeyVault();
        await using var firstContext = new DataContext(options);
        await using var secondContext = new DataContext(options);
        var replacements = new Dictionary<string, string>
        {
            ["api-key"] = "key", ["api-secret"] = "secret", ["webhook-secret"] = "webhook"
        };

        var results = await Task.WhenAll(
            new BybitCredentialSetService(firstContext, vault, NullLogger<BybitCredentialSetService>.Instance)
                .ReplaceAsync(1, 1, replacements, CancellationToken.None),
            new BybitCredentialSetService(secondContext, vault, NullLogger<BybitCredentialSetService>.Instance)
                .ReplaceAsync(1, 1, replacements, CancellationToken.None));

        results.Should().ContainSingle(result => result.Success);
        await using var verificationContext = new DataContext(options);
        var operations = await verificationContext.CredentialUpdateOperations.ToListAsync();
        var winner = operations.Should().ContainSingle(operation => operation.State == "Active").Which;
        var loser = operations.Should().ContainSingle(operation => operation.OperationId != winner.OperationId).Which;
        loser.State.Should().BeOneOf("Superseded", "RecoveryRequired");
        (await verificationContext.SyncStatuses.SingleAsync()).ActiveCredentialSetId.Should().Be(winner.NewCredentialSetId);
    }

    [Fact]
    public async Task ReplaceAsync_WhenExchangeIntegrationActivationRaces_ShouldKeepExactlyOneWinner()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using (var setupContext = new DataContext(options))
        {
            await setupContext.Database.MigrateAsync();
            var integration = new ExchangeIntegration(1, "Bybit");
            integration.ActivateCredentialSet("original-set");
            setupContext.ExchangeIntegrations.Add(integration);
            await setupContext.SaveChangesAsync();
        }

        using var vault = new ActivationBarrierKeyVault();
        await using var firstContext = new DataContext(options);
        await using var secondContext = new DataContext(options);
        var replacements = new Dictionary<string, string>
        {
            ["api-key"] = "key", ["api-secret"] = "secret", ["webhook-secret"] = "webhook"
        };

        var results = await Task.WhenAll(
            new BybitCredentialSetService(firstContext, vault, NullLogger<BybitCredentialSetService>.Instance)
                .ReplaceAsync(1, null, replacements, CancellationToken.None),
            new BybitCredentialSetService(secondContext, vault, NullLogger<BybitCredentialSetService>.Instance)
                .ReplaceAsync(1, null, replacements, CancellationToken.None));

        results.Should().ContainSingle(result => result.Success);
        await using var verificationContext = new DataContext(options);
        var operations = await verificationContext.CredentialUpdateOperations.ToListAsync();
        var winner = operations.Should().ContainSingle(operation => operation.State == "Active").Which;
        var loser = operations.Should().ContainSingle(operation => operation.OperationId != winner.OperationId).Which;
        loser.State.Should().BeOneOf("Superseded", "RecoveryRequired");
        (await verificationContext.ExchangeIntegrations.SingleAsync()).ActiveCredentialSetId.Should().Be(winner.NewCredentialSetId);
    }

    private sealed class ActivationBarrierKeyVault : IKeyVaultService, IDisposable
    {
        private readonly Barrier _activationBarrier = new(2);

        public Task<KeyVaultSecretReadResult> GetSecretReadResultAsync(string secretName) =>
            Task.FromResult(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));

        public Task<string?> GetSecretAsync(string secretName) => Task.FromResult<string?>(null);

        public async Task SetSecretAsync(string secretName, string value)
        {
            if (secretName.EndsWith("-webhook-secret", StringComparison.Ordinal))
                await Task.Run(() => _activationBarrier.SignalAndWait(TimeSpan.FromSeconds(30)));
        }

        public Task DeleteSecretAsync(string secretName) => Task.CompletedTask;

        public void Dispose() => _activationBarrier.Dispose();
    }
}
