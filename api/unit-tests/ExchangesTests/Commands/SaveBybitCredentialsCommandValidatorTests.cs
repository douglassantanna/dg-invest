using api.Exchanges.Commands;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitCredentialsCommandValidatorTests
{
    private readonly SaveBybitCredentialsCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenExistingAccountCredentialsAreComplete()
    {
        var command = new SaveBybitCredentialsCommand(1, 1, "api-key", "api-secret", "webhook-secret");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldPass_WhenExistingAccountEditDoesNotChangeCredentials()
    {
        var command = new SaveBybitCredentialsCommand(1, 1, "", "", "");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldFail_WhenUserIdIsInvalid(int userId)
    {
        var command = new SaveBybitCredentialsCommand(userId, 1, "key", "secret", "webhook");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
    }

    [Fact]
    public void ShouldFail_WhenAccountIdIsNegative()
    {
        var command = new SaveBybitCredentialsCommand(1, -1, "key", "secret", "webhook");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "AccountId");
    }

    [Fact]
    public void ShouldFail_WhenCreatingAccountWithoutApiKey()
    {
        var command = new SaveBybitCredentialsCommand(1, 0, "", "secret", "", "Futures", "UID-001");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ApiKey");
    }

    [Fact]
    public void ShouldFail_WhenCreatingAccountWithoutApiSecret()
    {
        var command = new SaveBybitCredentialsCommand(1, 0, "key", "", "", "Futures", "UID-001");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ApiSecret");
    }

    [Fact]
    public void ShouldFail_WhenUpdatingOnlyApiKey()
    {
        var command = new SaveBybitCredentialsCommand(1, 1, "replacement-key", "", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ApiSecret");
    }

    [Fact]
    public void ShouldFail_WhenUpdatingOnlyApiSecret()
    {
        var command = new SaveBybitCredentialsCommand(1, 1, "", "replacement-secret", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ApiKey");
    }

    [Fact]
    public void ShouldPass_WhenUpdatingOnlyWebhookSecret()
    {
        var command = new SaveBybitCredentialsCommand(1, 1, "", "", "replacement-webhook");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}
