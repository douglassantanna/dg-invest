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

    [JsonPropertyName("cumFeeDetail")]
    public Dictionary<string, string> CumFeeDetail { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

public class BybitExecutionData
{
    [JsonPropertyName("execId")]
    public string ExecId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("execPrice")]
    public string ExecPrice { get; set; } = string.Empty;

    [JsonPropertyName("execQty")]
    public string ExecQty { get; set; } = string.Empty;

    [JsonPropertyName("execValue")]
    public string ExecValue { get; set; } = string.Empty;

    [JsonPropertyName("execFee")]
    public string ExecFee { get; set; } = string.Empty;

    [JsonPropertyName("feeCurrency")]
    public string FeeCurrency { get; set; } = string.Empty;

    [JsonPropertyName("execTime")]
    public string ExecTime { get; set; } = string.Empty;
}
