using api.Cryptos.Models;
using api.Data;
using api.Data.Repositories;
using api.Shared;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Repositories;
public interface IUserPortfolioSnapshotsRepository
{
    Task<Response> GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            int userId,
            int accountId,
            long startTime,
            CancellationToken cancellationToken
        );
}
public class UserPortfolioSnapshots : IUserPortfolioSnapshotsRepository
{
    private readonly IBaseRepository<UserPortfolioSnapshot> _baseRepository;
    private readonly DataContext _dataContext;

    public UserPortfolioSnapshots(IBaseRepository<UserPortfolioSnapshot> baseRepository, DataContext dataContext)
    {
        _baseRepository = baseRepository;
        _dataContext = dataContext;
    }

    public async Task<Response> GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
        int userId,
        int accountId,
        long startTime,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var marketData = await _dataContext.UserPortfolioSnapshots
                                    .AsNoTracking()
                                    .Where(m => m.UserId == userId && m.Time >= startTime)
                                    .Where(x => x.AccountId == accountId)
                                    .ToListAsync(cancellationToken);

            if (marketData == null || !marketData.Any())
            {
                return new Response("No data found", false);
            }

            return new Response("", true, marketData);
        }
        catch (Exception ex)
        {
            return new Response("Error", false, ex.Message);
        }
    }
}
public record UserPortfolioSnapshotDto(long Time, decimal Value);
