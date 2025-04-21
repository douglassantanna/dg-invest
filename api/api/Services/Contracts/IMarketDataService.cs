namespace api.Services.Contracts;
public interface IMarketDataService
{
    Task FetchAndProcessMarketDataAsync(CancellationToken cancellationToken);
}
