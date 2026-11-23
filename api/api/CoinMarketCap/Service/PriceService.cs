using Microsoft.Extensions.Caching.Memory;

namespace api.CoinMarketCap.Service;

// One price per coin in the shared IMemoryCache, reused by every user and request for FreshFor (CoinMarketCap itself
// updates about once a minute). Entries are kept for KeepLastKnownFor so a CoinMarketCap failure can fall back to the
// last known price instead of showing zero.
public class PriceService : IPriceService
{
    public static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan KeepLastKnownFor = TimeSpan.FromHours(24);

    private readonly ICoinMarketCapService _coinMarketCap;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;
    private readonly ILogger<PriceService> _logger;

    public PriceService(ICoinMarketCapService coinMarketCap, IMemoryCache cache, TimeProvider clock, ILogger<PriceService> logger)
    {
        _coinMarketCap = coinMarketCap;
        _cache = cache;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PriceLookup> GetPricesAsync(IEnumerable<int> coinMarketCapIds, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow();
        var prices = new Dictionary<int, decimal>();
        var toFetch = new List<int>();

        foreach (var id in coinMarketCapIds.Distinct())
        {
            if (_cache.TryGetValue(Key(id), out CachedPrice? cached) && cached is not null && now - cached.FetchedAt < FreshFor)
                prices[id] = cached.Price;
            else
                toFetch.Add(id);
        }

        if (toFetch.Count == 0)
            return new PriceLookup(prices, FetchFailed: false);

        cancellationToken.ThrowIfCancellationRequested();
        GetQuoteResponse quotes;
        try
        {
            quotes = await _coinMarketCap.GetQuotesByIds(toFetch.Select(id => id.ToString()).ToArray());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "CoinMarketCap unavailable for {Count} coin(s); using last known prices", toFetch.Count);
            foreach (var id in toFetch)
            {
                if (_cache.TryGetValue(Key(id), out CachedPrice? lastKnown) && lastKnown is not null)
                    prices[id] = lastKnown.Price;
            }
            return new PriceLookup(prices, FetchFailed: true);
        }

        foreach (var id in toFetch)
        {
            if (quotes?.Data is null
                || !quotes.Data.TryGetValue(id.ToString(), out var coin)
                || coin.Quote?.USD?.Price is not { } price)
                continue;

            prices[id] = price;
            _cache.Set(Key(id), new CachedPrice(price, now), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = KeepLastKnownFor });
        }

        return new PriceLookup(prices, FetchFailed: false);
    }

    private static string Key(int coinMarketCapId) => $"price_{coinMarketCapId}";

    private sealed record CachedPrice(decimal Price, DateTimeOffset FetchedAt);
}
