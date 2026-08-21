using System.Text.Json;
using api.Controllers;
using api.Exchanges.Queries;
using api.Users.Dtos;
using api.Users.Commands;

namespace unit_tests.ExchangesTests;

public class ApiContractCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void SimpleAccountDto_ShouldSerializeNewAndLegacyAccountNames()
    {
        var payload = JsonSerializer.Serialize(new SimpleAccountDto(1, "Main", 100m, true), JsonOptions);

        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("name").GetString().Should().Be("Main");
        document.RootElement.GetProperty("subaccountTag").GetString().Should().Be("Main");
    }

    [Fact]
    public void ExchangeResponseDtos_ShouldSerializeNewAndLegacyFields()
    {
        var syncStatus = JsonSerializer.Serialize(
            new SyncStatusDto(1, "Futures", "Bybit", "Connected", null, null, 0, null),
            JsonOptions);
        var subaccount = JsonSerializer.Serialize(
            new BybitSubaccountRowDto(1, "Futures", "UID-001", "ok", true, true, false, null, "", null, true),
            JsonOptions);
        var subMember = JsonSerializer.Serialize(
            new BybitSubMemberDto("UID-001", "futures", "", "Futures", 1),
            JsonOptions);

        syncStatus.Should().Contain("\"accountName\":\"Futures\"");
        syncStatus.Should().Contain("\"accountTag\":\"Futures\"");
        subaccount.Should().Contain("\"externalId\":\"UID-001\"");
        subaccount.Should().Contain("\"bybitUid\":\"UID-001\"");
        subMember.Should().Contain("\"mappedAccountName\":\"Futures\"");
        subMember.Should().Contain("\"mappedAccountTag\":\"Futures\"");
    }

    [Fact]
    public void LegacyRequestFields_ShouldResolveToCurrentContract()
    {
        var createAccount = new CreateAccountRequest(null, "Legacy account");
        var credentials = new SaveBybitCredentialsRequest(0, "key", "secret", "", SubaccountTag: "Futures", BybitUid: "UID-001");
        var mapping = new MapBybitAccountRequest(1, BybitUid: "UID-001");

        createAccount.ResolvedName.Should().Be("Legacy account");
        credentials.ResolvedName.Should().Be("Futures");
        credentials.ResolvedExternalId.Should().Be("UID-001");
        mapping.ResolvedExternalId.Should().Be("UID-001");
    }
}
