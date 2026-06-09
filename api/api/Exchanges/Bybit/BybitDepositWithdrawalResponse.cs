using System.Text.Json.Serialization;

namespace api.Exchanges.Bybit;

public class BybitDepositHistoryResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitDepositWithdrawalResult Result { get; set; } = new();
}

public class BybitWithdrawalHistoryResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string RetMsg { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public BybitDepositWithdrawalResult Result { get; set; } = new();
}

public class BybitDepositWithdrawalResult
{
    [JsonPropertyName("rows")]
    public List<BybitDepositWithdrawalRow> Rows { get; set; } = new();

    [JsonPropertyName("nextPageCursor")]
    public string? NextPageCursor { get; set; }
}

public class BybitDepositWithdrawalRow
{
    [JsonPropertyName("coin")]
    public string Coin { get; set; } = string.Empty;

    [JsonPropertyName("chain")]
    public string Chain { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("txID")]
    public string TxId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("toAddress")]
    public string ToAddress { get; set; } = string.Empty;

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("depositFee")]
    public string DepositFee { get; set; } = string.Empty;

    [JsonPropertyName("withdrawFee")]
    public string WithdrawFee { get; set; } = string.Empty;

    [JsonPropertyName("successAt")]
    public string? SuccessAt { get; set; }

    [JsonPropertyName("confirmations")]
    public string Confirmations { get; set; } = string.Empty;

    [JsonPropertyName("txIndex")]
    public string TxIndex { get; set; } = string.Empty;

    [JsonPropertyName("blockHash")]
    public string BlockHash { get; set; } = string.Empty;
}
