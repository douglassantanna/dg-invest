using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Users.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading;
using Xunit;
using FluentAssertions;

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
        _handler = new SaveBybitCredentialsCommandHandler(_context, CredentialService(_context, _keyVaultMock.Object));
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
    public async Task Handle_WhenApiSecretEqualsApiKey_ShouldReturnValidationError()
    {
        _context.Accounts.Add(new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001"));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1, ApiKey: "same", ApiSecret: "same", WebhookSecret: ""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _keyVaultMock.Verify(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
    public async Task Handle_WhenKeyVaultWriteFails_ShouldReturnErrorWithoutSyncStatus()
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
        result.Message.Should().Contain("recovery may be required");
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCreatingAccount_AndKeyVaultFails_ShouldRetainAccountWithoutSyncStatus()
    {
        _keyVaultMock.Setup(v => v.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("failed"));

        var result = await _handler.Handle(new SaveBybitCredentialsCommand(1, 0, "key", "secret", "", "Futures", "UID-001"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("recovery may be required");
        var account = await _context.Accounts.SingleAsync();
        account.IsDeleted.Should().BeFalse();
        (await _context.SyncStatuses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCreatingUnmappedAccounts_ShouldPersistNullExternalIds()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new User("User", "user@example.com", "hash", Role.User));
        await context.SaveChangesAsync();
        var handler = new SaveBybitCredentialsCommandHandler(context, CredentialService(context, _keyVaultMock.Object));

        (await handler.Handle(new SaveBybitCredentialsCommand(1, 0, "key", "secret", "", "First", ""), CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await handler.Handle(new SaveBybitCredentialsCommand(1, 0, "key", "secret", "", "Second", "   "), CancellationToken.None)).IsSuccess.Should().BeTrue();

        (await context.Accounts.Where(account => account.Exchange == "Bybit" && account.ExternalId == null).CountAsync()).Should().Be(2);
        (await context.Accounts.Where(account => account.Exchange == "Bybit" && account.ExternalId != null && account.ExternalId != "").CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_WhenAccountProvided_ShouldWriteCanonicalSecretsAndEnableStatus()
    {
        _context.Accounts.Add(new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001"));
        await _context.SaveChangesAsync();
        var service = CredentialService(_context, _keyVaultMock.Object);

        var result = await service.SaveAsync(1, 1, "key", "secret", "webhook", BybitRegion.Global, CancellationToken.None);

        result.Success.Should().BeTrue();
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s == BybitCredentialKeys.LegacyAccountKey(1, 1, "api-key")), "key"), Times.Once);
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s == BybitCredentialKeys.LegacyAccountKey(1, 1, "api-secret")), "secret"), Times.Once);
        var status = await _context.SyncStatuses.SingleAsync();
        status.IsEnabled.Should().BeTrue();
        status.Region.Should().Be("Global");
    }

    [Fact]
    public async Task SaveAsync_WhenIntegration_ShouldWriteIntegrationSecretsAndEnable()
    {
        var service = CredentialService(_context, _keyVaultMock.Object);

        var result = await service.SaveAsync(1, null, "key", "secret", null, BybitRegion.Eu, CancellationToken.None);

        result.Success.Should().BeTrue();
        _keyVaultMock.Verify(v => v.SetSecretAsync(
            It.Is<string>(s => s == BybitCredentialKeys.LegacyIntegrationKey(1, "api-key")), "key"), Times.Once);
        var integration = await _context.ExchangeIntegrations.SingleAsync();
        integration.Enabled.Should().BeTrue();
        integration.Region.Should().Be("Eu");
    }

    private static IBybitCredentialSetService CredentialService(DataContext context, IKeyVaultService vault) =>
        new BybitCredentialSetService(context, vault, Mock.Of<ILogger<BybitCredentialSetService>>());
}
