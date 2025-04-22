using api.Shared;

namespace api.Services.Contracts;
public interface IMarketDataService
{
    Task<Result<bool>> FetchAndProcessMarketDataAsync(CancellationToken cancellationToken);
}
