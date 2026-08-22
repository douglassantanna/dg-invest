using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using Microsoft.Extensions.Logging;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitIntegrationCredentialsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateIntegrationAndStoreUserScopedSecrets()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(
            keyVault.Object, context, Mock.Of<ILogger<SaveBybitIntegrationCredentialsCommandHandler>>());

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "api-key", "api-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await context.ExchangeIntegrations.SingleAsync()).Exchange.Should().Be("Bybit");
        keyVault.Verify(x => x.SetSecretAsync(
            SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(1, "api-key"), "api-key"), Times.Once);
        keyVault.Verify(x => x.SetSecretAsync(
            SaveBybitIntegrationCredentialsCommandHandler.BuildIntegrationKey(1, "api-secret"), "api-secret"), Times.Once);
    }
}
