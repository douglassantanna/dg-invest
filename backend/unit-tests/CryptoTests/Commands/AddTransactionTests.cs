using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;
using api.Cryptos.Commands;
using api.Data;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Cache;

namespace api.Tests.Cryptos.Commands;
public class AddTransactionCommandTests
{
    private readonly Mock<ILogger<AddTransactionCommandHandler>> _loggerMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly DataContext _context;
    private readonly AddTransactionCommand _validCommand;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public AddTransactionCommandTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AddTransactionCommandHandler>>();
        _transactionServiceMock = new Mock<ITransactionService>();
        var options = new DbContextOptionsBuilder<DataContext>()
           .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
           .Options;
        _context = new DataContext(options);

        _validCommand = new AddTransactionCommand(
            Amount: 100m,
            Price: 50000m,
            PurchaseDate: DateTimeOffset.Now.AddDays(-1),
            ExchangeName: "Binance",
            TransactionType: ETransactionType.Buy,
            CryptoAssetId: 1,
            UserId: 1,
            Fee: 0.1m
        );
    }

    [Fact]
    public void Validator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(_validCommand);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_WhenAmountIsInvalid_ShouldFail(decimal amount)
    {
        // Arrange
        var command = _validCommand with { Amount = amount };
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Amount");
    }

    [Fact]
    public void Validator_WhenPurchaseDateIsInFuture_ShouldFail()
    {
        // Arrange
        var command = _validCommand with { PurchaseDate = DateTimeOffset.Now.AddDays(1) };
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PurchaseDate");
    }

    [Fact]
    public async Task Handler_WhenAccountNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(_validCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handler_WhenTransactionServiceFails_ShouldReturnError()
    {
        // Arrange
        var account = new Account("main", 1);
        account.AddCryptoAsset(new CryptoAsset("BTC", "USD", "Bitcoin", 1));
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("Transaction failed", false));

        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _context, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(_validCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}