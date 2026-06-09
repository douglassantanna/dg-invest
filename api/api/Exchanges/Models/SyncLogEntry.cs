namespace api.Exchanges.Models;

public record SyncLogEntry(
    string Id,
    int UserId,
    int AccountId,
    string ExchangeName,
    string OrderId,
    string Symbol,
    string Side,
    decimal Qty,
    decimal Price,
    string Status,
    string? ErrorMessage,
    DateTime Timestamp,
    string ImportSource);
