namespace api.Exchanges.Bybit;

public class BybitApiException : Exception
{
    public int RetCode { get; }
    public string RetMsg { get; }

    public BybitApiException(int retCode, string retMsg)
        : base($"Bybit returned error {retCode}: {retMsg}")
    {
        RetCode = retCode;
        RetMsg = retMsg;
    }
}
