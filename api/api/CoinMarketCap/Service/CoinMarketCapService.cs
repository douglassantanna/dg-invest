using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace api.CoinMarketCap.Service;
public class CoinMarketCapService : ICoinMarketCapService
{
    private readonly string quotesLatestEndpoint = "v2/cryptocurrency/quotes/latest";
    private readonly CoinMarketCapSettings _coinMarketCapSettings;
    private readonly ILogger<CoinMarketCapService> _logger;
    public CoinMarketCapService(IOptions<CoinMarketCapSettings> coinMarketCapSettings,
                                ILogger<CoinMarketCapService> logger)
    {
        _coinMarketCapSettings = coinMarketCapSettings.Value;
        _logger = logger;
    }
    public async Task<GetQuoteResponse> GetQuoteBySymbol(string symbol)
    {
        try
        {
            var raw = await _coinMarketCapSettings.BaseUrl
                                .AppendPathSegment(quotesLatestEndpoint)
                                .WithHeader(_coinMarketCapSettings.Header, _coinMarketCapSettings.ApiKey)
                                .SetQueryParam("symbol", symbol)
                                .GetStringAsync();

            var jObject = JObject.Parse(raw);
            var data = new Dictionary<string, Coin>();
            var dataToken = jObject["data"];
            if (dataToken is JObject dataObj)
            {
                foreach (var kvp in dataObj)
                {
                    var first = kvp.Value?.First;
                    if (first != null)
                    {
                        var coin = first.ToObject<Coin>();
                        if (coin != null)
                            data[kvp.Key] = coin;
                    }
                }
            }
            var status = jObject["status"]!.ToObject<Status>()!;
            return new GetQuoteResponse(status, data);
        }
        catch (FlurlHttpException ex)
        {
            var body = await ex.GetResponseStringAsync();
            _logger.LogError(ex, "GetQuoteBySymbol. Error calling CoinMarketCap. Status: {Status}, Body: {Body}", ex.StatusCode, body);
            throw;
        }
    }

    public async Task<GetQuoteResponse> GetQuotesByIds(string[] ids)
    {
        try
        {
            string idList = FormatSymbolList(ids);

            var raw = await _coinMarketCapSettings.BaseUrl
                                .AppendPathSegment(quotesLatestEndpoint)
                                .WithHeader(_coinMarketCapSettings.Header, _coinMarketCapSettings.ApiKey)
                                .SetQueryParam("id", idList)
                                .GetStringAsync();

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<GetQuoteResponse>(raw);
            return response!;
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "GetQuotesByIds. Error trying to call CoinMarketCap. Error: {0}", ex.Message);
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

    public decimal GetCryptoCurrencyPriceById(int coinMarketCapId, GetQuoteResponse cmpResponse)
    {
        if (cmpResponse?.Data != null)
        {
            if (cmpResponse.Data.TryGetValue(coinMarketCapId.ToString(), out var coin))
            {
                return coin.Quote.USD.Price ?? 0;
            }
        }
        return 0;
    }
}
