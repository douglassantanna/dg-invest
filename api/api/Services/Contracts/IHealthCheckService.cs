using api.Shared;

namespace api.Services.Contracts
{
    public interface IHealthCheckService
    {
        Task<Result<bool>> IsDatabaseHealthyAsync();
    }
}