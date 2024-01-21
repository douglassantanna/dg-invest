using api.Cryptos.Commands;
using api.Data.Repositories;
using api.Models.Cryptos;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.CryptoTests.Commands;
public class AddTransactionTests
{
    private readonly AddTransactionCommand _validCommand;
    private readonly Mock<IBaseRepository<CryptoAsset>> _cryptoAssetRepositoryMock;
    private readonly CryptoAsset _validCryptoAsset;
    private readonly Mock<ILogger<AddTransactionCommandHandler>> _loggerMock;

    public AddTransactionTests()
    {
        _validCommand = new AddTransactionCommand(Amount: 1,
                                                  Price: 10,
                                                  PurchaseDate: DateTimeOffset.Parse("2023-10-09"),
                                                  ExchangeName: "Binance",
                                                  TransactionType: ETransactionType.Buy,
                                                  CryptoAssetId: 1);

        _cryptoAssetRepositoryMock = new Mock<IBaseRepository<CryptoAsset>>();
        _validCryptoAsset = new CryptoAsset("BTC", "USD", "BTC", 1);
        _loggerMock = new Mock<ILogger<AddTransactionCommandHandler>>();
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
                                                CryptoAssetId: 1);

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
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).Returns((CryptoAsset?)null);

        // Act
        var handler = new AddTransactionCommandHandler(_cryptoAssetRepositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
    [Fact]
    public async void AddTransactionCommandHandler_WhenCryptoAssetIsFound_ShouldReturnTrue()
    {
        // Arrange
        var command = _validCommand;
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).Returns(_validCryptoAsset);
        _cryptoAssetRepositoryMock.Setup(x => x.UpdateAsync(_validCryptoAsset));

        // Act
        var handler = new AddTransactionCommandHandler(_cryptoAssetRepositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CryptoAsset? cryptoAsset = result.Data as CryptoAsset;
        cryptoAsset?.Transactions.Should().NotBeEmpty();
    }
    [Fact]
    public async void AddTransactionCommandHandler_WhenTransactionIdAdded_ShouldReturnTrue()
    {
        // Arrange
        var command = _validCommand;
        _cryptoAssetRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).Returns(_validCryptoAsset);
        _cryptoAssetRepositoryMock.Setup(x => x.UpdateAsync(_validCryptoAsset));

        // Act
        var handler = new AddTransactionCommandHandler(_cryptoAssetRepositoryMock.Object, _loggerMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        CryptoAsset? cryptoAsset = result.Data as CryptoAsset;
        cryptoAsset?.TotalInvested.Should().Be(_validCommand.Price);
    }
}
