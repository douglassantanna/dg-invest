using api.Cryptos.Commands;
using api.Cryptos.Repositories;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Models.Cryptos;
using api.Users.Models;
using api.Users.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.EntityFrameworkCore.Query;
using api.Cryptos.Models;
namespace unit_tests.CryptoTests.Commands;
public class AddTransactionTests
{
    private readonly AddTransactionCommand _validCommand;
    private readonly Mock<ICryptoAssetRepository> _cryptoAssetRepositoryMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly CryptoAsset _validCryptoAsset;
    private readonly User _validUser;
    private readonly Mock<ILogger<AddTransactionCommandHandler>> _loggerMock;

    public AddTransactionTests()
    {
        _validCommand = new AddTransactionCommand(Amount: 1,
                                                  Price: 10,
                                                  PurchaseDate: DateTimeOffset.Parse("2023-10-09"),
                                                  ExchangeName: "Binance",
                                                  TransactionType: ETransactionType.Buy,
                                                  CryptoAssetId: 1,
                                                  UserId: 1,
                                                  Fee: 0.0m);

        _cryptoAssetRepositoryMock = new Mock<ICryptoAssetRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _validCryptoAsset = new CryptoAsset("BTC", "USD", "BTC", 1);
        _loggerMock = new Mock<ILogger<AddTransactionCommandHandler>>();
        _validUser = new User(fullName: "Test", email: "test@gmail.com", password: "password", role: Role.Admin, new Account());
    }

    [Fact]
    public void AddTransactionCommand_WhenRequestIsValid_ShouldReturnTrue()
    {
        // Arrange
        var command = _validCommand;

        // Act
        var validator = new AddTransactionCommandValidator();
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
    [Fact]
    public void AddTransactionCommand_WhenPurchaseDateIsOnTheFuture_ShouldReturnFalse()
    {
        // Arrange
        var command = new AddTransactionCommand(Amount: 1,
                                                Price: 1,
                                                PurchaseDate: DateTimeOffset.Parse("2050-10-09"),
                                                ExchangeName: "Binance",
                                                TransactionType: ETransactionType.Buy,
                                                CryptoAssetId: 1,
                                                UserId: 1,
                                                Fee: 0.0m);

        // Act
        var validator = new AddTransactionCommandValidator();
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PurchaseDate");
    }
    [Fact]
    public async void AddTransactionCommandHandler_WhenCryptoAssetIsNull_ShouldReturnFalse()
    {
        // Arrange
        var command = _validCommand;
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), null)).ReturnsAsync((CryptoAsset?)null);

        // Act
        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _userRepositoryMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public async void AddTransactionCommandHandler_WhenCryptoAssetIsNotFound_ShouldReturnFalse()
    {
        // Arrange
        var command = _validCommand;
        _userRepositoryMock.Setup(x => x.GetByIdAsync(
            It.IsAny<int>(),
            It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()))
            .ReturnsAsync(_validUser);
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), null)).ReturnsAsync(_validCryptoAsset);
        _cryptoAssetRepositoryMock.Setup(x => x.UpdateAsync(_validCryptoAsset));

        // Act
        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _userRepositoryMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public async void AddTransactionCommandHandler_WhenTransactionIdAdded_ShouldReturnTrue()
    {
        // Arrange
        var command = _validCommand;
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), null)).ReturnsAsync(_validCryptoAsset);
        _cryptoAssetRepositoryMock.Setup(x => x.UpdateAsync(_validCryptoAsset));

        // Act
        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _userRepositoryMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        CryptoAsset? cryptoAsset = result.Data as CryptoAsset;
        cryptoAsset?.TotalInvested.Should().Be(_validCommand.Price);
    }
}
