using System.Net;
using System.Text.Json;
using api.CoinMarketCap;
using api.Data;
using api.Models.Cryptos;
using api.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace unit_tests.IntegrationTests;

[Collection(ExchangeApiIntegrationCollection.Name)]
public class RecalculatePreviewIntegrationTests
{
    private const int PreviewCoinMarketCapId = 9_101;
    private readonly ExchangeApiIntegrationFixture _fixture;

    public RecalculatePreviewIntegrationTests(ExchangeApiIntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Preview_RequiresAuthentication()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/Recalculate/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preview_ReturnsOnlyTheCallersAssetsWithBeforeAndAfterFigures()
    {
        var (userA, mainA) = await _fixture.CreateUserAsync();
        var (userB, mainB) = await _fixture.CreateUserAsync();
        var assetA = await AddBtcWithWithdrawalAsync(mainA);
        await AddBtcWithWithdrawalAsync(mainB);
        _fixture.Factory.CoinMarketCap.CoinsBySymbol["PREVIEWBTC"] = new Coin(
            PreviewCoinMarketCapId, "Preview Bitcoin", "PREVIEWBTC", DateTime.UtcNow, new Quote(new USD(80_000m, DateTime.UtcNow, 0)));

        try
        {
            using var client = _fixture.Factory.CreateAuthenticatedClient(userA);
            var response = await client.GetAsync("/api/Recalculate/preview");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var rows = json.RootElement.GetProperty("data").EnumerateArray().ToList();
            rows.Should().ContainSingle("user B's asset must not be visible to user A");
            var row = rows[0];
            row.GetProperty("cryptoAssetId").GetInt32().Should().Be(assetA);
            row.GetProperty("before").GetProperty("invested").GetDecimal().Should().Be(60_000m);
            row.GetProperty("after").GetProperty("invested").GetDecimal().Should().Be(30_000m);
            row.GetProperty("after").GetProperty("averagePrice").GetDecimal().Should().Be(60_000m);
        }
        finally
        {
            _fixture.Factory.CoinMarketCap.CoinsBySymbol.Remove("PREVIEWBTC");
        }

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        (await context.CryptoAssets.AsNoTracking().SingleAsync(asset => asset.Id == assetA))
            .TotalInvested.Should().Be(60_000m, "the preview must not save anything");
    }

    [Theory]
    [InlineData(Role.User, HttpStatusCode.Forbidden)]
    [InlineData(Role.Admin, HttpStatusCode.BadRequest)] // admins pass authorization; recalculation is disabled by default
    public async Task Recalculate_IsRestrictedToAdmins(Role role, HttpStatusCode expected)
    {
        var (userId, _) = await _fixture.CreateUserAsync(role);
        using var client = _fixture.Factory.CreateAuthenticatedClient(userId, role);

        var response = await client.PostAsync("/api/Recalculate/recalculate", null);

        response.StatusCode.Should().Be(expected);
    }

    // 1 BTC bought at 60k, then 0.5 BTC withdrawn. Stored with the Legacy policy, so the full cost stays on 0.5 BTC.
    private async Task<int> AddBtcWithWithdrawalAsync(int accountId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var account = await context.Accounts.Include(candidate => candidate.CryptoAssets).SingleAsync(candidate => candidate.Id == accountId);
        var asset = new CryptoAsset("Preview Bitcoin", "Preview Bitcoin", "PREVIEWBTC", PreviewCoinMarketCapId);
        asset.AddHistoricalTransaction(new CryptoTransaction(1m, 60_000m, DateTimeOffset.UtcNow.AddDays(-2), "Test", ETransactionType.Buy, 0m));
        asset.AddHistoricalTransaction(new CryptoTransaction(0.5m, 80_000m, DateTimeOffset.UtcNow.AddDays(-1), "Test", ETransactionType.TransferOut, 0m));
        account.AddCryptoAsset(asset).IsSuccess.Should().BeTrue();
        await context.SaveChangesAsync();
        return asset.Id;
    }
}
