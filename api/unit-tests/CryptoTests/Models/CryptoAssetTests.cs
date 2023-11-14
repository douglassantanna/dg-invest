using api.Cryptos.Models;
using api.Models.Cryptos;
using FluentAssertions;

namespace unit_tests.CryptoTests.Models;
public class CryptoAssetTests
{
    private readonly CryptoAsset _validCryptoAsset;

    public CryptoAssetTests()
    {
        _validCryptoAsset = new CryptoAsset("BTC", "USD", "BTC", 1);
    }

    [Fact]
    public void CreateCryptoAsset_WhenAddedAddress_AddressListShouldNotBeEmpty()
    {
        // Arrange
        var cryptoAsset = _validCryptoAsset;

        // Act
        cryptoAsset.AddAddress(new Address());

        // Assert
        cryptoAsset.Addresses.Should().HaveCount(1);
    }
}
