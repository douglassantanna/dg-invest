using api.AzureKeyVault;
using api.AzureStorage;
using api.AzureStorage.Blob;
using api.CoinMarketCap.Service;
using api.CoinMarketCap;
using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Models.Cryptos;
using api.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Commands;

public class ProcessBybitWebhookCommandHandlerTests
{
    private readonly Mock<IBybitService> _bybitMock;
    private readonly Mock<IKeyVaultService> _keyVaultMock;
    private readonly Mock<ICoinMarketCapService> _cmcMock;
    private readonly Mock<ITransactionService> _txMock;
    private readonly Mock<IBlobStorageService> _blobMock;
    private readonly DataContext _context;
    private readonly ProcessBybitWebhookCommandHandler _handler;
    private readonly ProcessBybitWebhookCommand _validCmd;
    private readonly BybitOrderData _filledOrder;

    public ProcessBybitWebhookCommandHandlerTests()
    {
        _bybitMock = new Mock<IBybitService>();
        _keyVaultMock = new Mock<IKeyVaultService>();
        _cmcMock = new Mock<ICoinMarketCapService>();
        _txMock = new Mock<ITransactionService>();
        _blobMock = new Mock<IBlobStorageService>();

        var storageSettings = Options.Create(new AzureStorageSettings
        {
            ConnectionString = "UseDevelopmentStorage=true",
            SyncLogsContainer = "sync-logs"
        });

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        var logger = Mock.Of<ILogger<ProcessBybitWebhookCommandHandler>>();

        _handler = new ProcessBybitWebhookCommandHandler(
            _bybitMock.Object,
            _keyVaultMock.Object,
            _cmcMock.Object,
            _txMock.Object,
            _blobMock.Object,
            storageSettings,
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

    private async Task<Account> SeedAccountAsync(bool includeEth = false)
    {
        var account = new Account("main", 1);
        account.AddCryptoAsset(new CryptoAsset("Bitcoin", "BTC", "BTC", 1));
        if (includeEth)
            account.AddCryptoAsset(new CryptoAsset("Ethereum", "ETH", "ETH", 1027));
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

        var syncStatus = await _context.SyncStatuses.FirstOrDefaultAsync();
        syncStatus.Should().NotBeNull();
        syncStatus!.Status.Should().Be("Error");
        syncStatus.ErrorCount.Should().Be(1);
        syncStatus.LastErrorMessage.Should().Be("Account not found");
    }

    [Fact]
    public async Task Handle_WhenOrderIsDuplicate_ShouldWriteDuplicateLog()
    {
        var account = await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _context.CryptoTransactions.Add(new CryptoTransaction(
            0.5m, 50000m, DateTimeOffset.Now, "Bybit",
            ETransactionType.Buy, 10m, "order-1"));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.Is<string>(p => p.Contains($"{account.Id}")),
            It.Is<SyncLogEntry>(e => e.Status == "Duplicate" && e.OrderId == "order-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderValuesUnparseable_ShouldWriteFailedLog()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var badOrder = CreateOrder(avgPrice: "not-a-number");
        var cmd = _validCmd with
        {
            Payload = new BybitWebhookPayload
            {
                Topic = "order",
                Data = [badOrder]
            }
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Status == "Failed" && e.ErrorMessage == "Could not parse numeric values"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCryptoAssetNotFoundAndCannotAutoCreate_ShouldWriteFailedLog()
    {
        await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _cmcMock
            .Setup(c => c.GetQuoteBySymbol("SOL"))
            .ReturnsAsync((GetQuoteResponse)null!);

        var unmatchedOrder = CreateOrder(symbol: "SOLUSDT", orderId: "order-sol");
        var cmd = _validCmd with
        {
            Payload = new BybitWebhookPayload
            {
                Topic = "order",
                Data = [unmatchedOrder]
            }
        };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Status == "Failed" && e.ErrorMessage!.Contains("Could not resolve asset")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTransactionStrategyFails_ShouldWriteFailedLog()
    {
        var account = await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _txMock
            .Setup(t => t.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("Insufficient balance", false));

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Status == "Failed" && e.ErrorMessage == "Insufficient balance"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderSucceeds_ShouldWriteSyncLogAndUpsertSyncStatus()
    {
        var account = await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _txMock
            .Setup(t => t.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("ok", true));

        var result = await _handler.Handle(_validCmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.Is<string>(p => p.Contains($"{account.Id}")),
            It.Is<SyncLogEntry>(e => e.Status == "Success" && e.OrderId == "order-1" && e.Symbol == "BTC" && e.Side == "Buy"),
            It.IsAny<CancellationToken>()), Times.Once);

        var syncStatus = await _context.SyncStatuses.FirstOrDefaultAsync();
        syncStatus.Should().NotBeNull();
        syncStatus!.Status.Should().Be("Connected");
        syncStatus.LastOrderId.Should().Be("order-1");
        syncStatus.ExchangeName.Should().Be("Bybit");
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_ShouldProcessAll()
    {
        var account = await SeedAccountAsync(includeEth: true);
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _txMock
            .Setup(t => t.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("ok", true));

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
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Status == "Success"),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        var syncStatus = await _context.SyncStatuses.FirstOrDefaultAsync();
        syncStatus!.LastOrderId.Should().Be("order-3");
    }

    [Fact]
    public async Task Handle_OnlyProcessesFilledOrders_SkipsOtherStatuses()
    {
        var account = await SeedAccountAsync();
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
        _txMock.Verify(t => t.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()), Times.Never);
        _blobMock.Verify(b => b.AppendLogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SyncLogEntry>(), It.IsAny<CancellationToken>()), Times.Never);

        var syncStatus = await _context.SyncStatuses.FirstOrDefaultAsync();
        syncStatus.Should().NotBeNull();
        syncStatus!.Status.Should().Be("Connected");
        syncStatus.LastOrderId.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_ExtractsBaseSymbol_Correctly()
    {
        var account = await SeedAccountAsync();
        _keyVaultMock
            .Setup(v => v.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("webhook-secret");
        _bybitMock
            .Setup(s => s.ValidateWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _txMock
            .Setup(t => t.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("ok", true));

        var testCases = new[] {
            ("BTCUSDT", "BTC"),
            ("ETHUSDC", "ETH"),
            ("XRPBUSD", "XRP"),
            ("SOLUSD", "SOL"),
        };

        foreach (var (symbol, expectedBase) in testCases)
        {
            var order = CreateOrder(symbol: symbol, orderId: $"order-{expectedBase}");
            var cmd = _validCmd with
            {
                Payload = new BybitWebhookPayload
                {
                    Topic = "order",
                    Data = [order]
                }
            };

            await _handler.Handle(cmd, CancellationToken.None);
        }

        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Symbol == "BTC"),
            It.IsAny<CancellationToken>()), Times.Once);
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Symbol == "ETH"),
            It.IsAny<CancellationToken>()), Times.Once);
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Symbol == "XRP"),
            It.IsAny<CancellationToken>()), Times.Once);
        _blobMock.Verify(b => b.AppendLogAsync(
            "sync-logs",
            It.IsAny<string>(),
            It.Is<SyncLogEntry>(e => e.Symbol == "SOL"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
