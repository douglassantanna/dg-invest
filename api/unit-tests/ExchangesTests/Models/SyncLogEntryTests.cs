using api.Exchanges.Models;

namespace unit_tests.ExchangesTests.Models;

public class SyncLogEntryTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var timestamp = DateTime.UtcNow;
        var entry = new SyncLogEntry(
            Id: "log-1",
            UserId: 1,
            AccountId: 2,
            ExchangeName: "Bybit",
            OrderId: "order-123",
            Symbol: "BTC",
            Side: "Buy",
            Qty: 0.5m,
            Price: 50000m,
            Status: "Success",
            ErrorMessage: null,
            Timestamp: timestamp,
            ImportSource: "Webhook");

        entry.Id.Should().Be("log-1");
        entry.UserId.Should().Be(1);
        entry.AccountId.Should().Be(2);
        entry.ExchangeName.Should().Be("Bybit");
        entry.OrderId.Should().Be("order-123");
        entry.Symbol.Should().Be("BTC");
        entry.Side.Should().Be("Buy");
        entry.Qty.Should().Be(0.5m);
        entry.Price.Should().Be(50000m);
        entry.Status.Should().Be("Success");
        entry.ErrorMessage.Should().BeNull();
        entry.Timestamp.Should().Be(timestamp);
        entry.ImportSource.Should().Be("Webhook");
    }

    [Fact]
    public void Constructor_WithError_ShouldSetErrorMessage()
    {
        var entry = new SyncLogEntry(
            Id: "log-2",
            UserId: 1,
            AccountId: 2,
            ExchangeName: "Bybit",
            OrderId: "order-456",
            Symbol: "ETH",
            Side: "Sell",
            Qty: 2m,
            Price: 3000m,
            Status: "Failed",
            ErrorMessage: "Insufficient balance",
            Timestamp: DateTime.UtcNow,
            ImportSource: "Webhook");

        entry.Status.Should().Be("Failed");
        entry.ErrorMessage.Should().Be("Insufficient balance");
    }
}
