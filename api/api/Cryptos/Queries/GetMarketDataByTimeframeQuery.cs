using api.Cryptos.Models;
using api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<IEnumerable<object>>;
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, IEnumerable<object>>
{
    private readonly DataContext _dbContext;
    public GetMarketDataByTimeframeQueryHandler(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<object>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long startTime = request.Timeframe switch
        {
            ETimeframe._24h => now - (24 * 60 * 60),
            ETimeframe._7d => now - (7 * 24 * 60 * 60),
            ETimeframe._1m => now - (30 * 24 * 60 * 60),
            ETimeframe._1y => now - (365 * 24 * 60 * 60),
            ETimeframe.All => 0,
            _ => throw new ArgumentException("Invalid timeframe")
        };

        var user = await _dbContext.Users.Include(x => x.Accounts).FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        var userAccount = user?.Accounts.Where(x => x.IsSelected).FirstOrDefault();

        var marketData = await _dbContext.MarketDataPoint
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId && m.Time >= startTime)
            .Where(x => x.AccountId == userAccount.Id)
            .ToListAsync(cancellationToken);

        var groupedData = marketData
            .GroupBy(m => (m.Time / 3600) * 3600)
            .Select(group => new
            {
                time = group.Key,
                value = group.Sum(m => m.CoinPrice)
            })
            .ToList();

        return groupedData;
    }
}
