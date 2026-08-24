using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Models.Cryptos;
using api.Shared;
using Microsoft.Extensions.Logging;

namespace unit_tests.ExchangesTests.Commands;

public class ProcessBybitWebhookCommandHandlerTests
{
    private readonly Mock<IBybitService> _bybitMock;
    private readonly Mock<IKeyVaultService> _keyVaultMock;
    private readonly Mock<IBybitOrderSyncService> _syncServiceMock;
    private readonly DataContext _context;
    private readonly ProcessBybitWebhookCommandHandler _handler;
    private readonly ProcessBybitWebhookCommand _validCmd;
    private readonly BybitOrderData _filledOrder;

    public ProcessBybitWebhookCommandHandlerTests()
    {
        _bybitMock = new Mock<IBybitService>();
        _keyVaultMock = new Mock<IKeyVaultService>();
        _keyVaultMock.Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "webhook-secret"));
        _syncServiceMock = new Mock<IBybitOrderSyncService>();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        var logger = Mock.Of<ILogger<ProcessBybitWebhookCommandHandler>>();

        _handler = new ProcessBybitWebhookCommandHandler(
            _bybitMock.Object,
            _keyVaultMock.Object,
            _syncServiceMock.Object,
            _context,
            logger);

        _filledOrder = CreateOrder();

        _validCmd = new ProcessBybitWebhookCommand(
            UserId: 1,
            AccountId: 1,
            Payload: new BybitWebhookPayload
            {
                Topic = "order",
                Data = [_filledOrder]
            },
            RawBody: "{\"topic\":\"order\",\"data\":[]}",
            Signature: "valid-sig",
            Timestamp: "1700000000000");
    }

    private static BybitOrderData CreateOrder(string? symbol = null, string? orderId = null, string? side = null, string? orderStatus = null, string? avgPrice = null, string? cumExecQty = null, string? cumExecFee = null, string? createdTime = null)
    {
        return new BybitOrderData
        {
            Symbol = symbol ?? "BTCUSDT",
            OrderId = orderId ?? "order-1",
            Side = side ?? "Buy",
            OrderStatus = orderStatus ?? "Filled",
            AvgPrice = avgPrice ?? "50000.5",
            CumExecQty = cumExecQty ?? "0.5",
            CumExecFee = cumExecFee ?? "10.0",
            CreatedTime = createdTime ?? "1700000000000"
        };
    }

    private async Task SeedIntegrationAsync()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        _context.ExchangeIntegrations.Add(integration);
        await _context.SaveChangesAsync();
    }

    private async Task<Account> SeedAccountAsync(bool seedStatus = true)
    {
        await SeedIntegrationAsync();
        var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
        account.AddCryptoAsset(new CryptoAsset("Bitcoin", "BTC", "BTC", 1));
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        if (seedStatus)
        {
            var status = new SyncStatus(1, account.Id, "Bybit");
            status.ActivateCredentialSet("account-set");
            _context.SyncStatuses.Add(status);
            await _context.SaveChangesAsync();
        }
        return account;
    }

    [Fact]
    public async Task Handle_WhenWebhookSecretMissing_ShouldReturn401()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(401);
        result.Message.Should().Be("Webhook secret not configured");
    }

    [Fact]
    public async Task Handle_WhenKeyVaultIsUnavailable_Returns503WithoutValidatingOrProcessing()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(503);
        _bybitMock.Verify(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _syncServiceMock.Verify(s => s.ProcessOrderAsync(It.IsAny<BybitOrderData>(), It.IsAny<Account>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _syncServiceMock.Verify(s => s.UpsertSyncStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignatureInvalid_ShouldReturn401()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(401);
        result.Message.Should().Be("Invalid signature");
    }

    [Fact]
    public async Task Handle_WhenTopicIsNotOrder_ShouldReturnOk()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var cmd = _validCmd with
        {
            Payload = new BybitWebhookPayload { Topic = "position", Data = [] }
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldAcknowledgeWithoutReadingVaultOrMarkingError()
    {
        await SeedIntegrationAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("ok");

        _keyVaultMock.Verify(v => v.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
        _syncServiceMock.Verify(s => s.MarkSyncStatusErrorAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderSucceeds_ShouldUpsertSyncStatus()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _syncServiceMock.Verify(s => s.ProcessOrderAsync(
            It.Is<BybitOrderData>(o => o.OrderId == "order-1"),
            It.IsAny<Account>(),
            1,
            "Webhook",
            It.IsAny<CancellationToken>()), Times.Once);

        _syncServiceMock.Verify(s => s.UpsertSyncStatusAsync(1, 1, "order-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithActiveCredentialSet_UsesImmutableWebhookSecretInsteadOfLegacyOrArbitraryKeys()
    {
        var account = await SeedAccountAsync(seedStatus: false);
        const string credentialSetId = "active-webhook-set";
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.ActivateCredentialSet(credentialSetId);
        _context.SyncStatuses.Add(status);
        await _context.SaveChangesAsync();

        var immutableKey = BybitCredentialKeys.SetKey(credentialSetId, "webhook-secret");
        var legacyKey = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "webhook-secret");
        var secrets = new Dictionary<string, string>
        {
            [immutableKey] = "immutable-webhook-secret",
            [legacyKey] = "conflicting-legacy-webhook-secret"
        };
        _keyVaultMock
            .Setup(v => v.GetSecretReadResultAsync(It.IsAny<string>()))
            .Returns((string key) => Task.FromResult(secrets.TryGetValue(key, out var value)
                ? new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, value)
                : throw new InvalidOperationException($"Unexpected vault key: {key}")));
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(_validCmd.RawBody, _validCmd.Signature, _validCmd.Timestamp, "immutable-webhook-secret"))
            .Returns(true);

        var result = await _handler.Handle(_validCmd with { AccountId = account.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _bybitMock.Verify(s => s.ValidateWebhookSignature(_validCmd.RawBody, _validCmd.Signature, _validCmd.Timestamp, "immutable-webhook-secret"), Times.Once);
        _keyVaultMock.Verify(v => v.GetSecretReadResultAsync(immutableKey), Times.Once);
        _keyVaultMock.Verify(v => v.GetSecretReadResultAsync(legacyKey), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSyncIsDisabled_ShouldAcknowledgeWithoutProcessingOrders()
    {
        await SeedAccountAsync(seedStatus: false);
        var syncStatus = new SyncStatus(1, 1, "Bybit");
        syncStatus.ActivateCredentialSet("disabled-set");
        syncStatus.ToggleEnabled();
        _context.SyncStatuses.Add(syncStatus);
        await _context.SaveChangesAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _syncServiceMock.Verify(s => s.ProcessOrderAsync(
            It.IsAny<BybitOrderData>(),
            It.IsAny<Account>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _syncServiceMock.Verify(s => s.UpsertSyncStatusAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_ShouldProcessAll()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var order2 = CreateOrder(symbol: "ETHUSDT", orderId: "order-2", side: "Buy");
        var order3 = CreateOrder(symbol: "BTCUSDT", orderId: "order-3", side: "Buy");
        var cmd = _validCmd with
        {
            Payload = new BybitWebhookPayload
            {
                Topic = "order",
                Data = [_filledOrder, order2, order3]
            }
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _syncServiceMock.Verify(s => s.ProcessOrderAsync(
            It.IsAny<BybitOrderData>(),
            It.IsAny<Account>(),
            1,
            "Webhook",
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        _syncServiceMock.Verify(s => s.UpsertSyncStatusAsync(1, 1, "order-3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnlyProcessesFilledOrders_SkipsOtherStatuses()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var cancelledOrder = CreateOrder(orderId: "order-cancelled", orderStatus: "Cancelled");
        var cmd = _validCmd with
        {
            Payload = new BybitWebhookPayload
            {
                Topic = "order",
                Data = [cancelledOrder]
            }
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _syncServiceMock.Verify(s => s.ProcessOrderAsync(
            It.IsAny<BybitOrderData>(),
            It.IsAny<Account>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _syncServiceMock.Verify(s => s.UpsertSyncStatusAsync(1, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
