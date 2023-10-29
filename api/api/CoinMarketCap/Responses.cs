namespace api.CoinMarketCap;
public record Status(int Error_code, string? Error_message);

public record Quote(decimal Price, DateTime Last_updated);

public record Coin(int Id,
                   string Name,
                   string Symbol,
                   DateTime Last_updated,
                   IDictionary<string, Quote>? Quote);


public record GetQuoteResponse(Status Status, Dictionary<string, List<Coin>> Data);

