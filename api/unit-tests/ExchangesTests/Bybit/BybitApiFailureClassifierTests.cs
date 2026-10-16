using System.Net.Http;
using api.Exchanges.Bybit;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitApiFailureClassifierTests
{
    [Theory]
    [InlineData(10003)]
    [InlineData(10004)]
    [InlineData(10005)]
    [InlineData(10007)]
    [InlineData(10010)]
    [InlineData(-2015)]
    [InlineData(33004)]
    public void Classify_BybitCredentialRejection_ReturnsPermanentCredentialFailure(int retCode)
    {
        var result = BybitApiFailureClassifier.Classify(new BybitApiException(retCode, "credential rejected"));

        result.Kind.Should().Be(BybitApiFailureKind.PermanentCredential);
        result.Code.Should().Be(retCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Classify_HttpRequestFailure_ReturnsTransportFailure()
    {
        var exception = new BybitTransportException("/v5/asset/deposit/query-record", new HttpRequestException("connection reset"));

        var result = BybitApiFailureClassifier.Classify(exception);

        result.Kind.Should().Be(BybitApiFailureKind.Transport);
        result.Endpoint.Should().Be("/v5/asset/deposit/query-record");
    }

    [Fact]
    public void Classify_RateLimitResponse_DoesNotReturnPermanentCredentialFailure()
    {
        var result = BybitApiFailureClassifier.Classify(new BybitApiException(10006, "rate limit"));

        result.Kind.Should().Be(BybitApiFailureKind.Other);
    }

    [Fact]
    public void Classify_CallerCancellation_IsNotATransportFailure()
    {
        var result = BybitApiFailureClassifier.Classify(new OperationCanceledException());

        result.Kind.Should().Be(BybitApiFailureKind.Other);
    }
}
