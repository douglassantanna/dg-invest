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

    private async Task<Account> SeedAccountAsync()
    {
        var account = new Account("main", 1);
        account.AddCryptoAsset(new CryptoAsset("Bitcoin", "BTC", "BTC", 1));
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    [Fact]
    public async Task Handle_WhenWebhookSecretMissing_ShouldReturn401()
    {
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(401);
        result.Message.Should().Be("Webhook secret not configured");
    }

    [Fact]
    public async Task Handle_WhenSignatureInvalid_ShouldReturn401()
    {
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
    public async Task Handle_WhenAccountNotFound_ShouldReturn404AndMarkSyncStatusError()
    {
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);

        _syncServiceMock.Verify(s => s.MarkSyncStatusErrorAsync(1, 1, "Account not found", It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task Handle_WhenSyncIsDisabled_ShouldAcknowledgeWithoutProcessingOrders()
    {
        await SeedAccountAsync();
        var syncStatus = new SyncStatus(1, 1, "Bybit");
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
