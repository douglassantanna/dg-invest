namespace unit_tests.CryptoTests.Commands;

public class WithdrawFundCommandHandlerTests
{
    private readonly Mock<ILogger<WithdrawFundCommandHandler>> _loggerMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly DataContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public WithdrawFundCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<WithdrawFundCommandHandler>>();
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
        var command = new WithdrawFundCommand(
            Amount: 100m,
            Date: DateTime.Now,
            UserId: 999,
            Notes: "test");

        var handler = new WithdrawFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WithFiatWithdrawal_ShouldSucceed()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("OK", true));

        var command = new WithdrawFundCommand(
            Amount: 200m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "bank withdrawal");

        var handler = new WithdrawFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCryptoWithdrawal_ShouldSucceed()
    {
        var account = new Account("main", 1);
        var cryptoAsset = new CryptoAsset("BTC", "USD", "Bitcoin", 1);
        account.AddCryptoAsset(cryptoAsset);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("OK", true));

        var command = new WithdrawFundCommand(
            Amount: 0.5m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "crypto withdrawal",
            TransactionType: EAccountTransactionType.WithdrawCrypto,
            CurrentPrice: 50000m,
            CryptoAssetId: cryptoAsset.Id.ToString(),
            ExchangeName: "Bybit");

        var handler = new WithdrawFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithCryptoWithdrawal_WhenAssetNotFound_ShouldReturn404()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var command = new WithdrawFundCommand(
            Amount: 0.5m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "crypto withdrawal",
            TransactionType: EAccountTransactionType.WithdrawCrypto,
            CurrentPrice: 50000m,
            CryptoAssetId: "999",
            ExchangeName: "Bybit");

        var handler = new WithdrawFundCommandHandler(
            _loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public void Validator_WhenFiatWithdrawal_ShouldNotRequireCryptoFields()
    {
        var command = new WithdrawFundCommand(
            Amount: 100m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "bank withdrawal");

        var validator = new WithdrawFundCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenCryptoWithdrawal_ShouldRequireCryptoFields()
    {
        var command = new WithdrawFundCommand(
            Amount: 0.1m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "crypto withdrawal",
            TransactionType: EAccountTransactionType.WithdrawCrypto);

        var validator = new WithdrawFundCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPrice");
        result.Errors.Should().Contain(e => e.PropertyName == "CryptoAssetId");
        result.Errors.Should().Contain(e => e.PropertyName == "ExchangeName");
    }

    [Fact]
    public void Validator_WhenAmountIsZero_ShouldFail()
    {
        var command = new WithdrawFundCommand(
            Amount: 0,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "test");

        var validator = new WithdrawFundCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }
}
