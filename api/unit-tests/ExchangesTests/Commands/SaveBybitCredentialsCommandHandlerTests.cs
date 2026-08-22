using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using Microsoft.Extensions.Logging;

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
}
