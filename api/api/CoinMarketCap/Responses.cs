namespace api.CoinMarketCap;
public record Status(int Error_code, string? Error_message);
public record USD(decimal Price, DateTime Last_updated);
public record Quote(USD USD);
public record Coin(int Id,
                   string Name,
                   string Symbol,
                   DateTime Last_updated,
                   Quote Quote);
public record GetQuoteResponse(Status Status, Dictionary<string, Coin> Data);

