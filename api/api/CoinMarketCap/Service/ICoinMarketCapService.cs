namespace api.CoinMarketCap.Service;
public interface ICoinMarketCapService
{
    Task<GetQuoteResponse> GetQuoteBySymbol(string symbol);
    Task<GetQuoteResponse> GetQuotesByIds(string[] ids);
    decimal GetCryptoCurrencyPriceById(int coinMarketCapId, GetQuoteResponse cmpResponse);
}
