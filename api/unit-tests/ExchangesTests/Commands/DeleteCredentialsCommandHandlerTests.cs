using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.Data.Sqlite;
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
    public async Task Handle_WhenAccountHasActiveImmutableSet_ShouldDeactivateAndRetireIt()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = new User("Test User", "test@example.com", "password", Role.User);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var account = new Account("Futures", user.Id, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var operation = new CredentialUpdateOperation(user.Id, "Bybit", account.Id, null, null);
        operation.MarkVaultWritten();
        var recoveryOperation = new CredentialUpdateOperation(user.Id, "Bybit", account.Id, operation.NewCredentialSetId, Guid.NewGuid());
        recoveryOperation.MarkRecoveryRequired("interrupted update");
        var status = new SyncStatus(user.Id, account.Id, "Bybit");
        status.ActivateCredentialSet(operation.NewCredentialSetId);
        context.AddRange(status, operation, recoveryOperation);
        await context.SaveChangesAsync();
        var vault = new Mock<IKeyVaultService>();
        var handler = new DeleteCredentialsCommandHandler(vault.Object, context, Mock.Of<ILogger<DeleteCredentialsCommandHandler>>());

        var result = await handler.Handle(new DeleteCredentialsCommand(user.Id, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();
        (await context.Accounts.FindAsync(account.Id))!.IsDeleted.Should().BeTrue();
        (await context.SyncStatuses.SingleAsync()).ActiveCredentialSetId.Should().BeNull();
        var operations = await context.CredentialUpdateOperations.OrderBy(x => x.CreatedAt).ToListAsync();
        operations.Should().ContainSingle(x => x.OperationId == operation.OperationId && x.State == "Retired");
        operations.Should().ContainSingle(x => x.OperationId == recoveryOperation.OperationId && x.State == "Superseded");
        var read = await BybitCredentialReader.ReadAsync(context, vault.Object, user.Id, account.Id, "api-key");
        read.IsFound.Should().BeFalse();
        vault.Verify(v => v.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLegacyCredentialRetirementFails_ShouldLeaveAccountAndActiveSetUntouched()
    {
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var status = new SyncStatus(1, 1, "Bybit");
        status.ActivateCredentialSet("active-set");
        _context.AddRange(account, status);
        await _context.SaveChangesAsync();
        _keyVault.Setup(v => v.SetSecretAsync(It.IsAny<string>(), string.Empty)).ThrowsAsync(new Exception("unavailable"));

        var result = await _handler.Handle(new DeleteCredentialsCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await _context.Accounts.FindAsync(account.Id))!.IsDeleted.Should().BeFalse();
        (await _context.SyncStatuses.SingleAsync()).ActiveCredentialSetId.Should().Be("active-set");
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
    public async Task Handle_WhenManualAccount_ShouldReject()
    {
        var account = new Account("Dad Account", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DeleteCredentialsCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Bybit exchange account");
        var saved = await _context.Accounts.FindAsync(account.Id);
        saved!.IsDeleted.Should().BeFalse();
        _keyVault.Verify(k => k.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnNotFound()
    {
        var result = await _handler.Handle(new DeleteCredentialsCommand(1, 999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }
}
