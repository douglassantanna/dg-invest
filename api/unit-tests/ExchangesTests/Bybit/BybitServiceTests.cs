using System.Security.Cryptography;
using System.Text;
using api.Exchanges.Bybit;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitServiceTests
{
    private readonly BybitService _sut;
    private readonly BybitSettings _settings;

    public BybitServiceTests()
    {
        _settings = new BybitSettings { UseTestnet = false };
        var options = Options.Create(_settings);
        var logger = Mock.Of<ILogger<BybitService>>();
        _sut = new BybitService(options, logger);
    }

    [Fact]
    public void ValidateWebhookSignature_WithValidSignature_ShouldReturnTrue()
    {
        var secret = "test-webhook-secret";
        var timestamp = "1700000000000";
        var body = @"{""topic"":""order"",""data"":[]}";
        var payload = $"{timestamp}{body}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var expectedSig = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var result = _sut.ValidateWebhookSignature(body, expectedSig, timestamp, secret);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateWebhookSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        var result = _sut.ValidateWebhookSignature(
            rawBody: "{}",
            signature: "invalid-signature",
            timestamp: "1700000000000",
            webhookSecret: "secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateWebhookSignature_WithWrongSecret_ShouldReturnFalse()
    {
        var secret = "real-secret";
        var timestamp = "1700000000000";
        var body = @"{""topic"":""order""}";
        var payload = $"{timestamp}{body}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var realSig = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var result = _sut.ValidateWebhookSignature(body, realSig, timestamp, "wrong-secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateWebhookSignature_WithDifferentTimestamp_ShouldReturnFalse()
    {
        var secret = "secret";
        var timestamp = "1700000000000";
        var body = @"{""topic"":""order""}";
        var payload = $"{timestamp}{body}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var sig = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var result = _sut.ValidateWebhookSignature(body, sig, "1700000000001", secret);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateWebhookSignature_WithTamperedBody_ShouldReturnFalse()
    {
        var secret = "secret";
        var timestamp = "1700000000000";
        var body = @"{""topic"":""order""}";
        var payload = $"{timestamp}{body}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var sig = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var result = _sut.ValidateWebhookSignature(@"{""topic"":""something-else""}", sig, timestamp, secret);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateWebhookSignature_WithEmptySignature_ShouldReturnFalse(string? signature)
    {
        var result = _sut.ValidateWebhookSignature("{}", signature ?? string.Empty, "1700000000000", "secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateWebhookSignature_WithCaseInsensitiveSignature_ShouldMatch()
    {
        var secret = "secret";
        var timestamp = "1700000000000";
        var body = @"{""topic"":""order""}";
        var payload = $"{timestamp}{body}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var expectedSig = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var resultUpper = _sut.ValidateWebhookSignature(body, expectedSig.ToUpperInvariant(), timestamp, secret);
        var resultLower = _sut.ValidateWebhookSignature(body, expectedSig.ToLowerInvariant(), timestamp, secret);

        resultUpper.Should().BeTrue();
        resultLower.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenBybitReturnsNonzeroRetCode_ShouldReturnFalse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var result = await _sut.TestConnectionAsync("api-key", "api-secret");

        result.Should().BeFalse();
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/account/info")
            .WithVerb(HttpMethod.Get);
    }
}
