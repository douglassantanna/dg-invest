using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;
public class BybitWebhookPayload
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("creationTime")]
    public string CreationTime { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<BybitOrderData> Data { get; set; } = new();
}

public class BybitOrderData
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("avgPrice")]
    public string AvgPrice { get; set; } = string.Empty;

    [JsonPropertyName("cumExecQty")]
    public string CumExecQty { get; set; } = string.Empty;

    [JsonPropertyName("cumExecFee")]
    public string CumExecFee { get; set; } = string.Empty;

    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}
