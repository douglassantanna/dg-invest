namespace api.Exchanges.Bybit;

public class BybitApiException : Exception
{
    public int RetCode { get; }
    public string RetMsg { get; }
    public string? Endpoint { get; }

    public BybitApiException(int retCode, string retMsg, string? endpoint = null)
        : base($"Bybit returned error {retCode}: {retMsg}")
    {
        RetCode = retCode;
        RetMsg = retMsg;
        Endpoint = endpoint;
    }
}

public sealed class BybitTransportException : Exception
{
    public string Endpoint { get; }

    public BybitTransportException(string endpoint, Exception innerException)
        : base($"Bybit transport request failed for {endpoint}", innerException)
    {
        Endpoint = endpoint;
    }
}

public enum BybitApiFailureKind
{
    Other,
    Transport,
    PermanentCredential
}

public sealed record BybitApiFailure(BybitApiFailureKind Kind, string? Code = null, string? Endpoint = null);

public static class BybitApiFailureClassifier
{
    private static readonly HashSet<int> PermanentCredentialCodes =
    [
        10003, // Invalid API key or environment mismatch
        10004, // Invalid signature
        10005, // Missing permission
        10007, // Authentication failed
        10010, // IP whitelist mismatch
        -2015, // Spot API key expired
        33004  // Derivatives API key expired
    ];

    public static BybitApiFailure Classify(Exception exception) => exception switch
    {
        BybitTransportException transport => new(BybitApiFailureKind.Transport, Endpoint: transport.Endpoint),
        BybitApiException apiException when PermanentCredentialCodes.Contains(apiException.RetCode)
            => new(BybitApiFailureKind.PermanentCredential,
                apiException.RetCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                apiException.Endpoint),
        _ => new(BybitApiFailureKind.Other)
    };

    public static bool IsPermanentCredentialCode(int retCode) => PermanentCredentialCodes.Contains(retCode);

    public static bool HasTransportFailure(Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            return false;

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.Net.Http.HttpRequestException
                or System.Net.Sockets.SocketException
                or TimeoutException)
                return true;

            if (current is TaskCanceledException && !cancellationToken.IsCancellationRequested)
                return true;
        }

        return false;
    }
}
