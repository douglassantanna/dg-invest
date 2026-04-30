using api.Cryptos.Models;

namespace unit_tests.Models;

public class AccountTests
{
    [Fact]
    public void Constructor_WhenCreatingAccount_ShouldInitializeProperties()
    {
        // Act
        var account = new Account("main", 1);

        // Assert
        account.SubaccountTag.Should().Be("main");
        account.UserId.Should().Be(1);
        account.IsSelected.Should().BeTrue(); // main account is selected by default
        account.CryptoAssets.Should().BeEmpty();
        account.Balance.Should().Be(0);
    }

    [Fact]
    public void Constructor_WhenCreatingSubaccount_ShouldNotBeSelected()
    {
        // Act
        var account = new Account("sub1", 1);

        // Assert
        account.SubaccountTag.Should().Be("sub1");
        account.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Select_WhenCalled_ShouldSetIsSelectedToTrue()
    {
        // Arrange
        var account = new Account("sub1", 1);

        // Act
        account.Select();

        // Assert
        account.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void Deselect_WhenCalled_ShouldSetIsSelectedToFalse()
    {
        // Arrange
        var account = new Account("main", 1);

        // Act
        account.Deselect();

        // Assert
        account.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void AddCryptoAsset_WhenAddingNewAsset_ShouldSucceed()
    {
        // Arrange
        var account = new Account("main", 1);
        var cryptoAsset = new CryptoAsset("BTC", "USD", "Bitcoin", 1);

        // Act
        var result = account.AddCryptoAsset(cryptoAsset);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CryptoAssets.Should().HaveCount(1);
        account.CryptoAssets.First().Should().Be(cryptoAsset);
    }

    [Fact]
    public void AddCryptoAsset_WhenAddingDuplicateAsset_ShouldFail()
    {
        // Arrange
        var account = new Account("main", 1);
        var cryptoAsset1 = new CryptoAsset("BTC", "USD", "Bitcoin", 1);
        var cryptoAsset2 = new CryptoAsset("BTC", "USD", "Bitcoin", 1);

        // Act
        var result1 = account.AddCryptoAsset(cryptoAsset1);
        var result2 = account.AddCryptoAsset(cryptoAsset2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeFalse();
        account.CryptoAssets.Should().HaveCount(1);
    }

    [Fact]
    public void TotalDeposited_WhenNoDeposits_ShouldReturnZero()
    {
        // Arrange
        var account = new Account("main", 1);

        // Assert
        account.TotalDeposited().Should().Be(0);
    }
}
