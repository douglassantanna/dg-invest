using api.Exchanges.Commands;

namespace unit_tests.ExchangesTests.Commands;

public class MapBybitAccountCommandValidatorTests
{
    private readonly MapBybitAccountCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: 5, BybitUid: "UID123");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldFail_WhenUserIdInvalid(int userId)
    {
        var cmd = new MapBybitAccountCommand(userId, AccountId: 5, BybitUid: "UID123");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldFail_WhenAccountIdInvalid(int accountId)
    {
        var cmd = new MapBybitAccountCommand(UserId: 1, accountId, "UID123");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFail_WhenBybitUidEmpty(string? uid)
    {
        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: 5, BybitUid: uid ?? string.Empty);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BybitUid");
    }

    [Fact]
    public void ShouldFail_WhenBybitUidTooLong()
    {
        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: 5, BybitUid: new string('U', 51));

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BybitUid");
    }
}
