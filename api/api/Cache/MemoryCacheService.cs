using Microsoft.Extensions.Caching.Memory;

namespace api.Cache;
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue(cacheKey, out T value))
        {
            value = await factory(cancellationToken);
            _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5)
            });
        }

        return value;
    }

    public void Remove(string cacheKey)
    {
        _cache.Remove(cacheKey);
    }

    public bool TryGetValue<T>(string cacheKey, out T value)
    {
        return _cache.TryGetValue(cacheKey, out value);
    }
}
