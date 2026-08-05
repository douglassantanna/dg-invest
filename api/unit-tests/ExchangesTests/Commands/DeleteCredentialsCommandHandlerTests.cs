using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.ExchangesTests.Commands;

public class DeleteCredentialsCommandHandlerTests
{
    private readonly DataContext _context;
    private readonly Mock<IKeyVaultService> _keyVault;
    private readonly DeleteCredentialsCommandHandler _handler;

    public DeleteCredentialsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _keyVault = new Mock<IKeyVaultService>();
        var logger = Mock.Of<ILogger<DeleteCredentialsCommandHandler>>();
        _handler = new DeleteCredentialsCommandHandler(_keyVault.Object, _context, logger);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ShouldSoftDeleteAndBlankSecrets()
    {
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DeleteCredentialsCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Accounts.FindAsync(account.Id);
        saved!.IsDeleted.Should().BeTrue();
        _keyVault.Verify(k => k.SetSecretAsync(It.IsAny<string>(), string.Empty), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WhenMainAccount_ShouldReject()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DeleteCredentialsCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("main account");
        var saved = await _context.Accounts.FindAsync(account.Id);
        saved!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnNotFound()
    {
        var result = await _handler.Handle(new DeleteCredentialsCommand(1, 999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }
}
