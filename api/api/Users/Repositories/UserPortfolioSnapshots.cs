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
            var snapshots = await _dataContext.UserPortfolioSnapshots
                                    .AsNoTracking()
                                    .Where(
                                        m => m.UserId == userId
                                        && m.Time >= startTime
                                        && m.AccountId == accountId)
                                    .ToListAsync(cancellationToken);

            if (snapshots == null || !snapshots.Any())
            {
                return new Response("No portfolio snapshots found for the specified criteria.", false);
            }

            return new Response("", true, snapshots);
        }
        catch (Exception ex)
        {
            return new Response("Failed to retrieve portfolio snapshots.", false, ex.Message);
        }
    }
}
public record UserPortfolioSnapshotDto(long Time, decimal Value);
