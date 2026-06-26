namespace unit_tests.CryptoTests.Commands;

public class DepositFundCommandHandlerTests
{
    private readonly Mock<ILogger<DepositFundCommandHandler>> _loggerMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly DataContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public DepositFundCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DepositFundCommandHandler>>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _cacheServiceMock = new Mock<ICacheService>();
        var options = new DbContextOptionsBuilder<DataContext>()
           .UseInMemoryDatabase(Guid.NewGuid().ToString())
           .Options;
        _context = new DataContext(options);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturn404()
    {
        var command = new DepositFundCommand(
            AccountTransactionType: EAccountTransactionType.DepositFiat,
            Amount: 100m,
            Date: DateTime.Now,
            UserId: 999,
            Notes: "test");

        var handler = new DepositFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WithFiatDeposit_ShouldSucceed()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("OK", true));

        var command = new DepositFundCommand(
            AccountTransactionType: EAccountTransactionType.DepositFiat,
            Amount: 500m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "fiat deposit");

        var handler = new DepositFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCryptoDeposit_ShouldSucceed()
    {
        var account = new Account("main", 1);
        var cryptoAsset = new CryptoAsset("BTC", "USD", "Bitcoin", 1);
        account.AddCryptoAsset(cryptoAsset);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("OK", true));

        var command = new DepositFundCommand(
            AccountTransactionType: EAccountTransactionType.DepositCrypto,
            Amount: 0.1m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "crypto deposit",
            CurrentPrice: 50000m,
            CryptoAssetId: cryptoAsset.Id.ToString(),
            ExchangeName: "Bybit");

        var handler = new DepositFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task Handle_WithCryptoDeposit_WhenAssetNotFound_ShouldReturn404()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var command = new DepositFundCommand(
            AccountTransactionType: EAccountTransactionType.DepositCrypto,
            Amount: 0.1m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "crypto deposit",
            CurrentPrice: 50000m,
            CryptoAssetId: "999",
            ExchangeName: "Bybit");

        var handler = new DepositFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }
}
