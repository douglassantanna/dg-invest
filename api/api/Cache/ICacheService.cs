namespace api.Cache;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);
    void Remove(string cacheKey);
    bool TryGetValue<T>(string cacheKey, out T value);
}
