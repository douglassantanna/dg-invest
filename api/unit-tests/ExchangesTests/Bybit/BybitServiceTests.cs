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
    public async Task TestConnectionAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.TestConnectionAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/account/info")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetSubAccountsAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetSubAccountsAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/user/submembers")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetOrderHistoryAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/order/history")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetExecutionHistoryAsync_ShouldSignTheExactQueryStringSentToBybit()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            retCode = 0,
            retMsg = "OK",
            result = new { list = Array.Empty<object>() }
        });

        const string apiKey = "api-key";
        const string apiSecret = "api-secret";
        var result = await _sut.GetExecutionHistoryAsync(apiKey, apiSecret, BybitRegion.Global, "order-1");

        result.Should().BeEmpty();
        var request = httpTest.CallLog.Should().ContainSingle().Subject.Request;
        var timestamp = request.Headers.First(header => header.Name == "X-BAPI-TIMESTAMP").Value;
        var expectedPayload = $"{timestamp}{apiKey}60000category=spot&limit=100&orderId=order-1";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var expectedSignature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedPayload))).ToLowerInvariant();

        request.Url.Query.Should().Be("category=spot&limit=100&orderId=order-1");
        request.Headers.First(header => header.Name == "X-BAPI-SIGN").Value.Should().Be(expectedSignature);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_ShouldFetchAllPages()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
            {
                retCode = 0,
                retMsg = "OK",
                result = new
                {
                    list = new[] { new { orderId = "order-1", symbol = "BTCUSDT" } },
                    nextPageCursor = "cursor-1"
                }
            })
            .RespondWithJson(new
            {
                retCode = 0,
                retMsg = "OK",
                result = new
                {
                    list = new[] { new { orderId = "order-2", symbol = "ETHUSDT" } },
                    nextPageCursor = ""
                }
            });

        var result = await _sut.GetOrderHistoryAsync("api-key", "api-secret", limit: 50);

        result.Select(order => order.OrderId).Should().Equal("order-1", "order-2");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/order/history")
            .WithQueryParam("cursor", "cursor-1")
            .Times(1);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_WhenCancellationIsAlreadyRequested_ShouldNotSendARequest()
    {
        using var httpTest = new HttpTest();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.GetOrderHistoryAsync("api-key", "api-secret", cancellationToken: cancellation.Token));

        httpTest.CallLog.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderHistoryAsync_ShouldProcessMoreThanOneHundredPages()
    {
        using var httpTest = new HttpTest();
        for (var page = 0; page <= 100; page++)
        {
            httpTest.RespondWithJson(new
            {
                retCode = 0,
                retMsg = "OK",
                result = new
                {
                    list = Array.Empty<object>(),
                    nextPageCursor = page == 100 ? "" : $"cursor-{page}"
                }
            });
        }

        var result = await _sut.GetOrderHistoryAsync("api-key", "api-secret");

        result.Should().BeEmpty();
        httpTest.CallLog.Should().HaveCount(101);
    }

    [Fact]
    public async Task GetDepositHistoryAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetDepositHistoryAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/deposit/query-record")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetWithdrawalHistoryAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetWithdrawalHistoryAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/withdraw/query-record")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetWalletBalanceAsync_WhenBybitReturnsBalance_ShouldReturnWalletResponse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            retCode = 0,
            retMsg = "OK",
            result = new
            {
                list = new[]
                {
                    new
                    {
                        accountType = "UNIFIED",
                        totalEquity = "10000.50",
                        coin = new[]
                        {
                            new { coin = "USDT", walletBalance = "6000.25", availableBalance = "5900.25", usdValue = "6000.25" },
                            new { coin = "USDC", walletBalance = "4000.25", availableBalance = "4000.25", usdValue = "4000.25" }
                        }
                    }
                }
            }
        });

        var result = await _sut.GetWalletBalanceAsync("api-key", "api-secret", BybitRegion.Global, "UNIFIED");

        result.Result.List.Should().ContainSingle();
        var wallet = result.Result.List.Single();
        wallet.AccountType.Should().Be("UNIFIED");
        wallet.TotalEquity.Should().Be("10000.50");
        wallet.Coin.Should().HaveCount(2);
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/account/wallet-balance")
            .WithQueryParam("accountType", "UNIFIED")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetWalletBalanceAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetWalletBalanceAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/account/wallet-balance")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetAccountCoinBalanceAsync_WhenBybitReturnsBalance_ShouldReturnSingleCoinBalance()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            retCode = 0,
            retMsg = "success",
            result = new
            {
                accountType = "UNIFIED",
                memberId = "12345",
                balance = new { coin = "USDT", walletBalance = "6000.25", transferBalance = "5900.25" }
            }
        });

        var result = await _sut.GetAccountCoinBalanceAsync("api-key", "api-secret", BybitRegion.Global, "UNIFIED", "USDT", "12345");

        result.Result.AccountType.Should().Be("UNIFIED");
        result.Result.MemberId.Should().Be("12345");
        result.Result.Balance.Coin.Should().Be("USDT");
        result.Result.Balance.WalletBalance.Should().Be("6000.25");
        result.Result.Balance.TransferBalance.Should().Be("5900.25");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-account-coin-balance")
            .WithQueryParam("accountType", "UNIFIED")
            .WithQueryParam("coin", "USDT")
            .WithQueryParam("memberId", "12345")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetAccountCoinBalanceAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetAccountCoinBalanceAsync("api-key", "api-secret", BybitRegion.Global, "FUND", "USDC"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-account-coin-balance")
            .WithQueryParam("accountType", "FUND")
            .WithQueryParam("coin", "USDC")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetInternalTransferHistoryAsync_WhenBybitReturnsTransfers_ShouldReturnRows()
    {
        using var httpTest = new HttpTest();
        httpTest
            .RespondWithJson(new
        {
            retCode = 0,
            retMsg = "success",
            result = new
            {
                list = new[]
                {
                    new
                    {
                        transferId = "transfer-1",
                        coin = "USDT",
                        amount = "250.50",
                        fromAccountType = "FUND",
                        toAccountType = "UNIFIED",
                        fromMemberId = "",
                        toMemberId = "12345",
                        timestamp = "1790000000000"
                    }
                },
                nextPageCursor = "next"
            }
            })
            .RespondWithJson(new
            {
                retCode = 0,
                retMsg = "success",
                result = new
                {
                    list = Array.Empty<object>(),
                    nextPageCursor = ""
                }
            });

        var result = await _sut.GetInternalTransferHistoryAsync("api-key", "api-secret", BybitRegion.Global, 50, 1700000000000);

        result.Should().ContainSingle();
        var transfer = result.Single();
        transfer.TransferId.Should().Be("transfer-1");
        transfer.Coin.Should().Be("USDT");
        transfer.Amount.Should().Be("250.50");
        transfer.FromAccountType.Should().Be("FUND");
        transfer.ToAccountType.Should().Be("UNIFIED");
        transfer.ToMemberId.Should().Be("12345");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-inter-transfer-list")
            .WithQueryParam("limit", "50")
            .WithQueryParam("startTime", "1700000000000")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetInternalTransferHistoryAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetInternalTransferHistoryAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-inter-transfer-list")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetUniversalTransferHistoryAsync_WhenBybitReturnsTransfers_ShouldReturnRows()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            retCode = 0,
            retMsg = "success",
            result = new
            {
                list = new[]
                {
                    new
                    {
                        transferId = "universal-transfer-1",
                        coin = "USDC",
                        amount = "125.50",
                        fromAccountType = "FUND",
                        toAccountType = "UNIFIED",
                        fromMemberId = "",
                        toMemberId = "12345",
                        timestamp = "1790000000000"
                    }
                }
            }
        });

        var result = await _sut.GetUniversalTransferHistoryAsync("api-key", "api-secret", BybitRegion.Global, 50, 1700000000000);

        result.Should().ContainSingle();
        var transfer = result.Single();
        transfer.TransferId.Should().Be("universal-transfer-1");
        transfer.Coin.Should().Be("USDC");
        transfer.ToMemberId.Should().Be("12345");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-universal-transfer-list")
            .WithQueryParam("limit", "50")
            .WithQueryParam("startTime", "1700000000000")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetUniversalTransferHistoryAsync_WhenBybitReturnsNonzeroRetCode_ShouldThrowBybitApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 10003, retMsg = "API key is invalid" });

        var exception = await Assert.ThrowsAsync<BybitApiException>(() => _sut.GetUniversalTransferHistoryAsync("api-key", "api-secret"));

        exception.RetCode.Should().Be(10003);
        exception.RetMsg.Should().Be("API key is invalid");
        httpTest.ShouldHaveCalled("https://api.bybit.com/v5/asset/transfer/query-universal-transfer-list")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetSubAccountsAsync_WhenRegionIsEu_ShouldCallEuHost()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 0, retMsg = "OK", result = new { subMembers = new object[] { } } });

        await _sut.GetSubAccountsAsync("api-key", "api-key", BybitRegion.Eu);

        httpTest.ShouldHaveCalled("https://api.bybit.eu/v5/user/submembers")
            .WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenRegionIsEuAndTestnet_ShouldCallEuTestnetHost()
    {
        var settings = new BybitSettings { UseTestnet = true };
        var sut = new BybitService(Options.Create(settings), Mock.Of<ILogger<BybitService>>());
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new { retCode = 0, retMsg = "OK" });

        await sut.TestConnectionAsync("api-key", "api-secret", BybitRegion.Eu);

        httpTest.ShouldHaveCalled("https://api-testnet.bybit.eu/v5/account/info")
            .WithVerb(HttpMethod.Get);
    }
}
