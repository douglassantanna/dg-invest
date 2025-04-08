using api.Cryptos.Models;
using api.Data;
using api.Data.Repositories;
using api.Shared;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Repositories;
public interface IUserPortfolioSnapshotsRepository
{
    Task<Result<List<UserPortfolioSnapshot>>> GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
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

    public async Task<Result<List<UserPortfolioSnapshot>>> GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
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
                return Result<List<UserPortfolioSnapshot>>.Failure(
                    "No portfolio snapshots found for the specified criteria."
                );
            }

            return Result<List<UserPortfolioSnapshot>>.Success(snapshots);
        }
        catch (Exception ex)
        {
            return Result<List<UserPortfolioSnapshot>>.Failure(
                $"An error occurred while retrieving portfolio snapshots: {ex.Message}"
            );
        }
    }
}
public record UserPortfolioSnapshotDto(long Time, decimal Value);
