using api.Exchanges.Models;

namespace unit_tests.ExchangesTests.Models;

public class ExchangeIntegrationAutoPauseTests
{
    [Fact]
    public void RecordTransportFailure_PausesOnlyAfterTenConsecutiveFailures()
    {
        var integration = new ExchangeIntegration(1, "Bybit");

        for (var attempt = 1; attempt < ExchangeIntegration.TransportFailurePauseThreshold; attempt++)
        {
            integration.RecordTransportFailure("transport", "request failed", "/v5/asset/deposit/query-record", 27, DateTime.UtcNow);
            integration.Enabled.Should().BeTrue();
        }

        integration.RecordTransportFailure("transport", "request failed", "/v5/asset/deposit/query-record", 27, DateTime.UtcNow)
            .Should().BeTrue();

        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be(ExchangeIntegration.AutoPausedStatus);
        integration.ConsecutiveTransportFailures.Should().Be(ExchangeIntegration.TransportFailurePauseThreshold);
        integration.AutoPausedAt.Should().NotBeNull();
        integration.LastErrorEndpoint.Should().Be("/v5/asset/deposit/query-record");
    }

    [Fact]
    public void RecordPermanentCredentialFailure_PausesImmediately()
    {
        var integration = new ExchangeIntegration(1, "Bybit");

        integration.RecordPermanentCredentialFailure("10003", "Invalid API key", "/v5/asset/deposit/query-record", 27, DateTime.UtcNow)
            .Should().BeTrue();

        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be(ExchangeIntegration.AutoPausedStatus);
        integration.ConsecutiveTransportFailures.Should().Be(0);
        integration.LastErrorCode.Should().Be("10003");
    }

    [Fact]
    public void MarkSyncCycleSucceeded_ResetsTransportFailureStreak()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordTransportFailure("transport", "request failed", "/v5/asset/deposit/query-record", 27, DateTime.UtcNow);

        integration.MarkSyncCycleSucceeded(DateTime.UtcNow);

        integration.ConsecutiveTransportFailures.Should().Be(0);
        integration.Status.Should().Be("Healthy");
        integration.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkNonTransportFailure_DoesNotCountOrResetTransportFailures()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordTransportFailure("transport", "request failed", "/v5/order/history", 27, DateTime.UtcNow);

        integration.MarkNonTransportFailure("Processing failed", 27, DateTime.UtcNow);

        integration.ConsecutiveTransportFailures.Should().Be(1);
        integration.Status.Should().Be("Error");
    }

    [Fact]
    public void MarkEnabled_DoesNotResumeAnAutoPausedIntegration()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordPermanentCredentialFailure("10003", "Invalid API key", "/v5/order/history", 27, DateTime.UtcNow);

        integration.MarkEnabled();
        integration.ToggleEnabled();

        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be(ExchangeIntegration.AutoPausedStatus);
    }

    [Fact]
    public void PauseNotification_WhenRepeatedlyUnsent_AllowsOnlyThreeAttemptsOneMinuteApart()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordPermanentCredentialFailure("10003", "Invalid API key", "/v5/order/history", 27, DateTime.UtcNow);
        var firstAttempt = DateTime.UtcNow;

        integration.CanAttemptPauseNotification(firstAttempt).Should().BeTrue();
        integration.RecordPauseNotificationAttempt(firstAttempt, sent: false);
        integration.CanAttemptPauseNotification(firstAttempt.AddSeconds(59)).Should().BeFalse();

        var secondAttempt = firstAttempt.AddMinutes(1);
        integration.CanAttemptPauseNotification(secondAttempt).Should().BeTrue();
        integration.RecordPauseNotificationAttempt(secondAttempt, sent: false);

        var thirdAttempt = secondAttempt.AddMinutes(1);
        integration.CanAttemptPauseNotification(thirdAttempt).Should().BeTrue();
        integration.RecordPauseNotificationAttempt(thirdAttempt, sent: false);

        integration.CanAttemptPauseNotification(thirdAttempt.AddMinutes(1)).Should().BeFalse();
        integration.PauseNotificationAttempts.Should().Be(3);
    }
}
