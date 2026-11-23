namespace api.CoinMarketCap.Service;

// Current USD prices shared by every user and request. Each coin's price is reused for PriceService.FreshFor; only
// coins missing or expired are fetched, in one CoinMarketCap call. When that call fails, the last known price is used.
public interface IPriceService
{
    Task<PriceLookup> GetPricesAsync(IEnumerable<int> coinMarketCapIds, CancellationToken cancellationToken = default);
}

// Prices: coins with a known price (fresh, or last known when CoinMarketCap failed). A coin CoinMarketCap does not list
// is absent. FetchFailed: CoinMarketCap could not be reached for some coins, so an absent coin may only be unknown now.
public sealed record PriceLookup(IReadOnlyDictionary<int, decimal> Prices, bool FetchFailed)
{
    public decimal PriceOf(int coinMarketCapId) => Prices.GetValueOrDefault(coinMarketCapId);
}
