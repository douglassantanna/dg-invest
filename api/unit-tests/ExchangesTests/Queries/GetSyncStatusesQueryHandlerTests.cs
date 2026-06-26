using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Models;
using api.Exchanges.Queries;

namespace unit_tests.ExchangesTests.Queries;

public class GetSyncStatusesQueryHandlerTests
{
    private readonly DataContext _context;
    private readonly GetSyncStatusesQueryHandler _handler;

    public GetSyncStatusesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _handler = new GetSyncStatusesQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenNoStatuses_ShouldReturnEmptyList()
    {
        var result = await _handler.Handle(new GetSyncStatusesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Data as List<SyncStatusDto>;
        data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenStatusExists_ShouldReturnJoinedWithAccountTag()
    {
        var account = new Account("main", 1);
        _context.Accounts.Add(account);
        _context.SyncStatuses.Add(new SyncStatus(1, account.Id, "Bybit"));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new GetSyncStatusesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = result.Data as List<SyncStatusDto>;
        data.Should().HaveCount(1);
        data![0].AccountId.Should().Be(account.Id);
        data[0].AccountTag.Should().Be("main");
        data[0].ExchangeName.Should().Be("Bybit");
        data[0].Status.Should().Be("Disconnected");
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnStatusesForRequestedUser()
    {
        var account1 = new Account("user1", 1);
        var account2 = new Account("user2", 2);
        _context.Accounts.AddRange(account1, account2);
        _context.SyncStatuses.Add(new SyncStatus(1, account1.Id, "Bybit"));
        _context.SyncStatuses.Add(new SyncStatus(2, account2.Id, "Bybit"));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new GetSyncStatusesQuery(1), CancellationToken.None);

        var data = result.Data as List<SyncStatusDto>;
        data.Should().HaveCount(1);
        data![0].AccountId.Should().Be(account1.Id);
    }
}
