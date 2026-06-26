using api.Exchanges.Models;

namespace unit_tests.ExchangesTests.Models;

public class SyncStatusTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var sut = new SyncStatus(1, 2, "Bybit");

        sut.UserId.Should().Be(1);
        sut.AccountId.Should().Be(2);
        sut.ExchangeName.Should().Be("Bybit");
        sut.Status.Should().Be("Disconnected");
        sut.LastSyncAt.Should().BeNull();
        sut.LastOrderId.Should().BeNull();
        sut.ErrorCount.Should().Be(0);
        sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkConnected_ShouldUpdateStatusAndResetErrors()
    {
        var sut = new SyncStatus(1, 2, "Bybit");
        sut.MarkError("something broke");
        sut.ErrorCount.Should().Be(1);

        sut.MarkConnected("order-123");

        sut.Status.Should().Be("Connected");
        sut.LastSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        sut.LastOrderId.Should().Be("order-123");
        sut.ErrorCount.Should().Be(0);
        sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkConnected_WithEmptyOrderId_ShouldStillSetConnected()
    {
        var sut = new SyncStatus(1, 2, "Bybit");

        sut.MarkConnected(string.Empty);

        sut.Status.Should().Be("Connected");
        sut.LastOrderId.Should().Be(string.Empty);
    }

    [Fact]
    public void MarkError_ShouldIncrementErrorCount()
    {
        var sut = new SyncStatus(1, 2, "Bybit");

        sut.MarkError("first error");
        sut.Status.Should().Be("Error");
        sut.ErrorCount.Should().Be(1);
        sut.LastErrorMessage.Should().Be("first error");

        sut.MarkError("second error");
        sut.Status.Should().Be("Error");
        sut.ErrorCount.Should().Be(2);
        sut.LastErrorMessage.Should().Be("second error");
    }

    [Fact]
    public void MarkError_WithNullMessage_ShouldSetNull()
    {
        var sut = new SyncStatus(1, 2, "Bybit");

        sut.MarkError(null);

        sut.Status.Should().Be("Error");
        sut.ErrorCount.Should().Be(1);
        sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkDisconnected_ShouldUpdateStatus()
    {
        var sut = new SyncStatus(1, 2, "Bybit");
        sut.MarkConnected("order-1");

        sut.MarkDisconnected("connection lost");

        sut.Status.Should().Be("Disconnected");
        sut.LastErrorMessage.Should().Be("connection lost");
    }

    [Fact]
    public void MarkDisconnected_WithNullMessage_ShouldSetNull()
    {
        var sut = new SyncStatus(1, 2, "Bybit");

        sut.MarkDisconnected(null);

        sut.Status.Should().Be("Disconnected");
        sut.LastErrorMessage.Should().BeNull();
    }
}
