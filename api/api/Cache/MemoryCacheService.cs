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
        if (_cache.TryGetValue<object>(cacheKey, out _))
        {
            _cache.Remove(cacheKey);
            Console.WriteLine($"Cache key {cacheKey} found and removed.");
        }
        else
        {
            Console.WriteLine($"Cache key {cacheKey} not found.");
        }
    }

    public bool TryGetValue<T>(string cacheKey, out T value)
    {
        return _cache.TryGetValue(cacheKey, out value);
    }
}
