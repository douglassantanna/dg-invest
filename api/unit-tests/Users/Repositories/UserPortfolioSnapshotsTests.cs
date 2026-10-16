using api.Data;
using api.Data.Repositories;
using api.Users.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace unit_tests.Users.Repositories;

public sealed class UserPortfolioSnapshotsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DataContext _context;
    private readonly UserPortfolioSnapshots _sut;

    public UserPortfolioSnapshotsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DataContext(options);
        _context.Database.EnsureCreated();
        _sut = new UserPortfolioSnapshots(
            Mock.Of<IBaseRepository<api.Cryptos.Models.UserPortfolioSnapshot>>(),
            _context);
    }

    [Fact]
    public async Task GetPortfolioSnapshots_WhenNoSnapshotsExist_ReturnsSuccessfulEmptyCollection()
    {
        var result = await _sut.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            userId: 1,
            accountId: 2,
            startTime: 100,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Empty(result.Value);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
