using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitCredentialsCommandHandlerTests
{
    private readonly Mock<IKeyVaultService> _keyVaultMock;
    private readonly DataContext _context;
    private readonly SaveBybitCredentialsCommandHandler _handler;

    public SaveBybitCredentialsCommandHandlerTests()
    {
        _keyVaultMock = new Mock<IKeyVaultService>();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _keyVaultMock
            .Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var logger = Mock.Of<ILogger<SaveBybitCredentialsCommandHandler>>();
        _handler = new SaveBybitCredentialsCommandHandler(_keyVaultMock.Object, _context, logger);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ShouldSaveToKeyVaultAndReturnSuccess()
    {
        _context.Accounts.Add(new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001"));
        await _context.SaveChangesAsync();
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1,
            ApiKey: "my-api-key",
            ApiSecret: "my-api-secret",
            WebhookSecret: "my-webhook-secret");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Credentials saved successfully");
        (await _context.SyncStatuses.SingleAsync()).BybitCredentialsSetAt.Should().NotBeNull();
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s.EndsWith("api-key")), "my-api-key"), Times.Once);
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s.EndsWith("api-secret")), "my-api-secret"), Times.Once);
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s.EndsWith("webhook-secret")), "my-webhook-secret"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnNotFound()
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 99, AccountId: 999,
            ApiKey: "key", ApiSecret: "secret", WebhookSecret: "webhook");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenEditFieldsAreBlank_ShouldPreserveStoredSecrets()
    {
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var command = new SaveBybitCredentialsCommand(1, account.Id, "", "", "");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("No credential changes supplied");
        _keyVaultMock.Verify(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKeyVaultThrows_ShouldReturnError()
    {
        _context.Accounts.Add(new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001"));
        await _context.SaveChangesAsync();
        _keyVaultMock
            .Setup(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Key Vault unavailable"));

        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1,
            ApiKey: "key", ApiSecret: "secret", WebhookSecret: "webhook");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(500);
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ShouldReturnValidationErrors()
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 0, AccountId: 0,
            ApiKey: "", ApiSecret: "", WebhookSecret: "");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Validation failed");
        result.Data.Should().BeOfType<List<string>>();
    }

    [Fact]
    public async Task Handle_WhenAccountIsManual_ShouldRejectWithoutWritingSecrets()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(1, account.Id, "key", "secret", ""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Bybit exchange account");
        _keyVaultMock.Verify(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFirstWriteFailsCreatingAccount_ShouldRetainAccountAndRecordRecovery()
    {
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "old"));
        var calls = 0;
        _keyVaultMock.Setup(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, _) => ++calls == 1 ? Task.FromException(new Exception("failed")) : Task.CompletedTask);

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(1, 0, "key", "secret", "", "Futures", "UID-001"), CancellationToken.None);

        result.Data.Should().Be(500);
        var account = await _context.Accounts.SingleAsync();
        account.IsDeleted.Should().BeFalse();
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
        var operation = await _context.CredentialUpdateOperations.SingleAsync();
        operation.State.Should().Be("RecoveryRequired");
        operation.CreatesAccount.Should().BeTrue();
        operation.AccountId.Should().Be(account.Id);
    }

    [Fact]
    public async Task Handle_WhenSecondWriteFailsCreatingAccount_ShouldRetainAccountAndImmutableSet()
    {
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var calls = 0;
        _keyVaultMock.Setup(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, _) => ++calls == 2 ? Task.FromException(new Exception("failed")) : Task.CompletedTask);

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(1, 0, "key", "secret", "", "Futures", "UID-001"), CancellationToken.None);

        result.Data.Should().Be(500);
        var account = await _context.Accounts.SingleAsync();
        account.IsDeleted.Should().BeFalse();
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
        var operation = await _context.CredentialUpdateOperations.SingleAsync();
        operation.State.Should().Be("RecoveryRequired");
        operation.CreatesAccount.Should().BeTrue();
        operation.AccountId.Should().Be(account.Id);
        _keyVaultMock.Verify(v => v.SetSecretAsync(It.Is<string>(key => key.StartsWith("bybit-set-")), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenPriorReadIsUnavailable_ShouldNotWriteOrPersistSyncStatus()
    {
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(1, account.Id, "key", "secret", ""), CancellationToken.None);

        result.Data.Should().Be(503);
        _keyVaultMock.Verify(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReplaceAsync_WhenPointerChangesBeforeActivation_ShouldPreserveWinnerAndRequireRecovery()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);
        await context.Database.EnsureCreatedAsync();
        var status = new SyncStatus(1, 1, "Bybit");
        status.ActivateCredentialSet("original-set");
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var vault = new Mock<IKeyVaultService>();
        var changedPointer = false;
        vault.Setup(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>(async (_, _) =>
            {
                if (changedPointer) return;
                changedPointer = true;
                await using var winnerContext = new DataContext(options);
                var winner = await winnerContext.SyncStatuses.SingleAsync();
                winner.ActivateCredentialSet("winning-set");
                await winnerContext.SaveChangesAsync();
            });
        var service = new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance);

        var result = await service.ReplaceAsync(1, 1, new Dictionary<string, string>
        {
            ["api-key"] = "key", ["api-secret"] = "secret", ["webhook-secret"] = "webhook"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        context.ChangeTracker.Clear();
        (await context.SyncStatuses.SingleAsync()).ActiveCredentialSetId.Should().Be("winning-set");
        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("RecoveryRequired");
    }

    [Fact]
    public async Task ReconcileAsync_WhenVaultWrittenSetIsComplete_ShouldActivateMatchingPriorPointer()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);
        await context.Database.EnsureCreatedAsync();
        var status = new SyncStatus(1, 1, "Bybit");
        status.ActivateCredentialSet("old-set");
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();
        var operation = new CredentialUpdateOperation(1, "Bybit", 1, "old-set", status.CredentialVersion);
        operation.MarkVaultWritten();
        context.CredentialUpdateOperations.Add(operation);
        await context.SaveChangesAsync();
        var vault = new Mock<IKeyVaultService>();
        vault.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "value"));

        var reconciled = await new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance)
            .ReconcileAsync(CancellationToken.None);

        reconciled.Should().Be(1);
        context.ChangeTracker.Clear();
        (await context.SyncStatuses.SingleAsync()).ActiveCredentialSetId.Should().Be(operation.NewCredentialSetId);
        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("Active");
    }

    [Fact]
    public async Task ReconcileAsync_WhenNewAccountOperationHasCompleteSet_ShouldCreateAndActivateSyncStatus()
    {
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        var operation = new CredentialUpdateOperation(1, "Bybit", account.Id, null, null, createsAccount: true);
        operation.MarkRecoveryRequired("second vault write failed");
        _context.CredentialUpdateOperations.Add(operation);
        await _context.SaveChangesAsync();
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "value"));

        await new BybitCredentialSetService(_context, _keyVaultMock.Object, NullLogger<BybitCredentialSetService>.Instance)
            .ReconcileAsync(CancellationToken.None);

        (await _context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("Active");
        var status = await _context.SyncStatuses.SingleAsync();
        status.AccountId.Should().Be(account.Id);
        status.ActiveCredentialSetId.Should().Be(operation.NewCredentialSetId);
    }

    [Fact]
    public async Task ReconcileAsync_WhenAnotherSetIsActive_ShouldMarkOperationSuperseded()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);
        await context.Database.EnsureCreatedAsync();
        var status = new SyncStatus(1, 1, "Bybit");
        status.ActivateCredentialSet("old-set");
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();
        var operation = new CredentialUpdateOperation(1, "Bybit", 1, "old-set", status.CredentialVersion);
        operation.MarkVaultWritten();
        status.ActivateCredentialSet("winning-set");
        context.CredentialUpdateOperations.Add(operation);
        await context.SaveChangesAsync();
        var vault = new Mock<IKeyVaultService>();
        vault.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "value"));

        await new BybitCredentialSetService(context, vault.Object, NullLogger<BybitCredentialSetService>.Instance)
            .ReconcileAsync(CancellationToken.None);

        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("Superseded");
    }

    [Theory]
    [InlineData(KeyVaultSecretReadStatus.NotFound, "Cleaned")]
    [InlineData(KeyVaultSecretReadStatus.Unavailable, "RecoveryRequired")]
    public async Task ReconcileAsync_WhenCredentialSetCannotBeVerified_ShouldNotActivate(KeyVaultSecretReadStatus status, string expectedState)
    {
        var operation = new CredentialUpdateOperation(1, "Bybit", 1, "old-set", Guid.NewGuid());
        operation.MarkVaultWritten();
        _context.CredentialUpdateOperations.Add(operation);
        await _context.SaveChangesAsync();
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(status));

        await new BybitCredentialSetService(_context, _keyVaultMock.Object, NullLogger<BybitCredentialSetService>.Instance)
            .ReconcileAsync(CancellationToken.None);

        (await _context.CredentialUpdateOperations.SingleAsync()).State.Should().Be(expectedState);
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void ConfigureServices_RegistersCredentialRecoveryHostedService()
    {
        var services = new ServiceCollection();

        services.ConfigureServices();

        services.Should().Contain(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(CredentialRecoveryService));
    }
}
