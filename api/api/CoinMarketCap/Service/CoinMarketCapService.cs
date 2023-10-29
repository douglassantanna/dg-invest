using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace api.CoinMarketCap.Service;
public class CoinMarketCapService : ICoinMarketCapService
{
    private readonly string endpoint = "v2/cryptocurrency/quotes/latest";
    private readonly CoinMarketCapSettings _coinMarketCapSettings;
    public CoinMarketCapService(IOptions<CoinMarketCapSettings> coinMarketCapSettings)
    {
        _coinMarketCapSettings = coinMarketCapSettings.Value;
    }
    public async Task<GetQuoteResponse> GetQuoteBySymbol(string symbol)
    {
        try
        {
            var response = await _coinMarketCapSettings.BaseUrl
                                .AppendPathSegment(endpoint)
                                .WithHeader(_coinMarketCapSettings.Header, _coinMarketCapSettings.ApiKey)
                                .SetQueryParam("symbol", symbol)
                                .GetJsonAsync<GetQuoteResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }

    }

    public async Task<GetQuoteResponse> GetQuotesBySymbols(string[] symbols)
    {
        try
        {
            string symbolList = FormatSymbolList(symbols);

            string bb = "";
            foreach (var item in symbolList)
            {
                bb += item;
            }
            string baseer = $"{_coinMarketCapSettings.BaseUrl} + {endpoint} + {bb}";

            var response = await _coinMarketCapSettings.BaseUrl
                                .AppendPathSegment(endpoint)
                                .WithHeader(_coinMarketCapSettings.Header, _coinMarketCapSettings.ApiKey)
                                .SetQueryParam("symbol", symbolList)
                                .GetJsonAsync<GetQuoteResponse>();

            return response;

        }
        catch (FlurlHttpException ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private static string FormatSymbolList(string[] symbols)
    {
        for (var i = 0; i < symbols.Length; i++)
        {
            symbols[i] = symbols[i].ToUpper();
        }

        string symbolList = string.Join(",", symbols);
        return symbolList;
    }
}
