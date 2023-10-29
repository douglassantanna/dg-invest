using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace api.CoinMarketCap.Service;
public class CoinMarketCapService : ICoinMarketCapService
{
    private readonly string quotesLatestEndpoint = "v2/cryptocurrency/quotes/latest";
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
                                .AppendPathSegment(quotesLatestEndpoint)
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

    public async Task<GetQuoteResponse> GetQuotesByIds(string[] ids)
    {
        try
        {
            string idList = FormatSymbolList(ids);

            var response = await _coinMarketCapSettings.BaseUrl
                                .AppendPathSegment(quotesLatestEndpoint)
                                .WithHeader(_coinMarketCapSettings.Header, _coinMarketCapSettings.ApiKey)
                                .SetQueryParam("id", idList)
                                .GetJsonAsync<GetQuoteResponse>();

            return response;

        }
        catch (FlurlHttpException ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private static string FormatSymbolList(string[] ids)
    {
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = ids[i].ToUpper();
        }

        string symbolList = string.Join(",", ids);
        return symbolList;
    }
}
