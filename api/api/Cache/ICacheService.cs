namespace api.Cache;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    void Remove(string cacheKey);
    bool TryGetValue<T>(string cacheKey, out T value);
}
