namespace unit_tests.Validators;

public class CommandValidatorTests
{
    [Fact]
    public void AddTransactionCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new AddTransactionCommandValidator();
        var command = new AddTransactionCommand(
            Amount: 100m,
            Price: 50000m,
            PurchaseDate: DateTimeOffset.Now.AddDays(-1),
            ExchangeName: "Binance",
            TransactionType: ETransactionType.Buy,
            CryptoAssetId: 1,
            UserId: 1,
            Fee: 0.1m);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTransactionCommandValidator_WhenAmountIsInvalid_ShouldFail(decimal amount)
    {
        // Arrange
        var validator = new AddTransactionCommandValidator();
        var command = new AddTransactionCommand(
            Amount: amount,
            Price: 50000m,
            PurchaseDate: DateTimeOffset.Now.AddDays(-1),
            ExchangeName: "Binance",
            TransactionType: ETransactionType.Buy,
            CryptoAssetId: 1,
            UserId: 1,
            Fee: 0.1m);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Amount");
    }

    [Fact]
    public void AddTransactionCommandValidator_WhenPurchaseDateIsInFuture_ShouldFail()
    {
        // Arrange
        var validator = new AddTransactionCommandValidator();
        var command = new AddTransactionCommand(
            Amount: 100m,
            Price: 50000m,
            PurchaseDate: DateTimeOffset.Now.AddDays(1),
            ExchangeName: "Binance",
            TransactionType: ETransactionType.Buy,
            CryptoAssetId: 1,
            UserId: 1,
            Fee: 0.1m);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PurchaseDate");
    }

    [Fact]
    public void CreateCryptoCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new CreateCryptoCommandValidator();
        var command = new CreateCryptoCommand(
            Name: "Bitcoin",
            Symbol: "BTC",
            Image: "https://example.com/btc.png",
            CoinMarketCapId: 1);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateCryptoCommandValidator_WhenNameIsEmpty_ShouldFail(string name)
    {
        // Arrange
        var validator = new CreateCryptoCommandValidator();
        var command = new CreateCryptoCommand(
            Name: name,
            Symbol: "BTC",
            Image: "https://example.com/btc.png",
            CoinMarketCapId: 1);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void AuthenticateCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new AuthenticateCommandValidator();
        var command = new AuthenticateCommand(
            Email: "test@example.com",
            Password: "password123");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AuthenticateCommandValidator_WhenEmailIsEmpty_ShouldFail(string email)
    {
        // Arrange
        var validator = new AuthenticateCommandValidator();
        var command = new AuthenticateCommand(
            Email: email,
            Password: "password123");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
    }

    [Fact]
    public void AuthenticateCommandValidator_WhenPasswordIsTooShort_ShouldFail()
    {
        // Arrange
        var validator = new AuthenticateCommandValidator();
        var command = new AuthenticateCommand(
            Email: "test@example.com",
            Password: "12345");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Password");
    }

    [Fact]
    public void WithdrawFundCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new WithdrawFundCommandValidator();
        var command = new WithdrawFundCommand(
            Amount: 100m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "Withdrawal");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithdrawFundCommandValidator_WhenAmountIsInvalid_ShouldFail(decimal amount)
    {
        // Arrange
        var validator = new WithdrawFundCommandValidator();
        var command = new WithdrawFundCommand(
            Amount: amount,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "Withdrawal");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Amount");
    }

    [Fact]
    public void DepositFundCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new DepositFundCommandValidator();
        var command = new DepositFundCommand(
            AccountTransactionType: EAccountTransactionType.DepositFiat,
            Amount: 1000m,
            Date: DateTime.Now,
            UserId: 1,
            Notes: "Deposit");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddCryptoAssetToAccountListCommandValidator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new AddCryptoAssetToAccountListCommandValidator();
        var command = new AddCryptoAssetToAccountListCommand(
            UserId: 1,
            CoinMarketCapId: 1,
            Symbol: "BTC");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AddCryptoAssetToAccountListCommandValidator_WhenSymbolIsEmpty_ShouldFail(string symbol)
    {
        // Arrange
        var validator = new AddCryptoAssetToAccountListCommandValidator();
        var command = new AddCryptoAssetToAccountListCommand(
            UserId: 1,
            CoinMarketCapId: 1,
            Symbol: symbol);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Symbol");
    }
}
