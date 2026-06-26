using api.Exchanges.Commands;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitCredentialsCommandValidatorTests
{
    private readonly SaveBybitCredentialsCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1,
            AccountId: 1,
            ApiKey: "valid-api-key",
            ApiSecret: "valid-api-secret",
            WebhookSecret: "valid-webhook-secret");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldFail_WhenUserIdInvalid(int userId)
    {
        var cmd = new SaveBybitCredentialsCommand(
            userId, AccountId: 1, "key", "secret", "webhook");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldFail_WhenAccountIdInvalid(int accountId)
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, accountId, "key", "secret", "webhook");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFail_WhenApiKeyEmpty(string? apiKey)
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1, apiKey ?? string.Empty, "secret", "webhook");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiKey");
    }

    [Fact]
    public void ShouldFail_WhenApiKeyTooLong()
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1, new string('a', 256), "secret", "webhook");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFail_WhenApiSecretEmpty(string? secret)
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1, "key", secret ?? string.Empty, "webhook");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiSecret");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFail_WhenWebhookSecretEmpty(string? webhookSecret)
    {
        var cmd = new SaveBybitCredentialsCommand(
            UserId: 1, AccountId: 1, "key", "secret", webhookSecret ?? string.Empty);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WebhookSecret");
    }
}
