using api.AzureStorage;
using api.AzureStorage.Blob;
using api.Exchanges.Models;
using api.Exchanges.Queries;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Queries;

public class GetSyncLogsQueryHandlerTests
{
    private readonly Mock<IBlobStorageService> _blobMock;
    private readonly GetSyncLogsQueryHandler _handler;

    public GetSyncLogsQueryHandlerTests()
    {
        _blobMock = new Mock<IBlobStorageService>();
        var settings = Options.Create(new AzureStorageSettings
        {
            ConnectionString = "UseDevelopmentStorage=true",
            LogContainer = "logs"
        });
        _handler = new GetSyncLogsQueryHandler(_blobMock.Object, settings);
    }

    [Fact]
    public async Task Handle_ShouldReadBlobWithDefaultDate()
    {
        var expectedPath = $"{DateTime.UtcNow:yyyy/MM/dd}/sync.jsonl";

        _blobMock
            .Setup(b => b.ReadLogsAsync<SyncLogEntry>("logs", expectedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetSyncLogsQuery(1, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Data as List<SyncLogEntry>;
        data.Should().BeEmpty();
        _blobMock.Verify(b => b.ReadLogsAsync<SyncLogEntry>("logs", expectedPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDateProvided_ShouldUseCustomDate()
    {
        var expectedPath = "2026/06/01/sync.jsonl";

        _blobMock
            .Setup(b => b.ReadLogsAsync<SyncLogEntry>("logs", expectedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetSyncLogsQuery(1, 5, "2026-06-01"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _blobMock.Verify(b => b.ReadLogsAsync<SyncLogEntry>("logs", expectedPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLogsExist_ShouldReturnThem()
    {
        var expectedPath = "2026/06/01/sync.jsonl";
        var logs = new List<SyncLogEntry>
        {
            new("log-1", 1, 5, "Bybit", "order-1", "BTC", "Buy", 0.5m, 50000m, "Success", null, DateTime.UtcNow, "Webhook"),
            new("log-2", 1, 5, "Bybit", "order-2", "ETH", "Sell", 2m, 3000m, "Success", null, DateTime.UtcNow, "Webhook"),
            new("log-3", 2, 5, "Bybit", "order-3", "SOL", "Buy", 1m, 100m, "Success", null, DateTime.UtcNow, "Webhook"),
        };

        _blobMock
            .Setup(b => b.ReadLogsAsync<SyncLogEntry>("logs", expectedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _handler.Handle(new GetSyncLogsQuery(1, 5, "2026-06-01"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Data as List<SyncLogEntry>;
        data.Should().HaveCount(2);
        data![0].OrderId.Should().Be("order-1");
        data[1].OrderId.Should().Be("order-2");
    }
}
