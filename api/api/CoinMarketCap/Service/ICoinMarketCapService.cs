namespace api.CoinMarketCap.Service;
public interface ICoinMarketCapService
{
    Task<GetQuoteResponse> GetQuoteBySymbol(string symbol);
}
