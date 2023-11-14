using api.Cryptos.Commands;
using api.Data.Repositories;
using api.Models.Cryptos;
using FluentAssertions;
using Moq;

namespace unit_tests.CryptoTests.Commands;
public class AddTransactionTests
{
    [Fact]
    public void AddTransactionCommand_WhenRequestIsValid_ShouldReturnTrue()
    {
        // Arrange
        var command = new AddTransactionCommand(Amount: 1,
                                                Price: 1,
                                                PurchaseDate: DateTimeOffset.Parse("2023-10-09"),
                                                ExchangeName: "Binance",
                                                TransactionType: ETransactionType.Buy,
                                                CryptoAssetId: 1);

        // Act
        var validator = new AddTransactionCommandValidator();
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
