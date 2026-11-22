using Flurl.Http;

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

// Bybit is throttling requests: HTTP 403 (IP limit, lifted after about 10 minutes) or 429.
public sealed class BybitRateLimitException : Exception
{
    public string Endpoint { get; }
    public int StatusCode { get; }

    public BybitRateLimitException(string endpoint, int statusCode, Exception innerException)
        : base($"Bybit rate limit (HTTP {statusCode}) for {endpoint}", innerException)
    {
        Endpoint = endpoint;
        StatusCode = statusCode;
    }
}

public enum BybitApiFailureKind
{
    Other,
    Transport,
    PermanentCredential,
    // Not a fault of the key or the network: the run stops and the next one retries. Never counts toward auto-pause.
    RateLimited
}

public sealed record BybitApiFailure(BybitApiFailureKind Kind, string? Code = null, string? Endpoint = null);

public static class BybitApiFailureClassifier
{
    private static readonly HashSet<int> PermanentCredentialCodes =
    [
        10003, // Invalid API key or environment mismatch
        10004, // Invalid signature
        10007, // Authentication failed
        10010, // IP whitelist mismatch
        -2015, // Spot API key expired
        33004  // Derivatives API key expired
    ];

    private static readonly HashSet<int> RateLimitCodes =
    [
        10006, // Too many visits (per-UID limit)
        10018  // Exceeded the IP rate limit
    ];

    public static BybitApiFailure Classify(Exception exception) => exception switch
    {
        BybitTransportException transport => new(BybitApiFailureKind.Transport, Endpoint: transport.Endpoint),
        BybitRateLimitException rateLimit => new(BybitApiFailureKind.RateLimited,
            rateLimit.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture), rateLimit.Endpoint),
        BybitApiException apiException when RateLimitCodes.Contains(apiException.RetCode)
            => new(BybitApiFailureKind.RateLimited,
                apiException.RetCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                apiException.Endpoint),
        BybitApiException apiException when apiException.RetCode == 10005
            => new(BybitApiFailureKind.Other,
                apiException.RetCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                apiException.Endpoint),
        BybitApiException apiException when PermanentCredentialCodes.Contains(apiException.RetCode)
            => new(BybitApiFailureKind.PermanentCredential,
                apiException.RetCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                apiException.Endpoint),
        _ => new(BybitApiFailureKind.Other)
    };

    public static bool IsRateLimit(Exception exception) => Classify(exception).Kind == BybitApiFailureKind.RateLimited;

    public static bool IsPermanentCredentialCode(int retCode) => PermanentCredentialCodes.Contains(retCode);

    public static bool HasTransportFailure(Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            return false;

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is FlurlHttpException { StatusCode: int statusCode }
                && (statusCode == 408 || statusCode >= 500))
                return true;

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

// Messages shown in the app (sync status, API responses) for the Bybit errors users actually hit. The code stays in the
// message for troubleshooting; unknown codes keep Bybit's own text.
public static class BybitErrorMessages
{
    public static string Describe(BybitApiException exception, string context) => exception.RetCode switch
    {
        10003 => $"Bybit rejected the API key: it is invalid, was deleted, or belongs to the other region (Global/EU). Create a new read-only key and save it again. [Bybit {exception.RetCode}]",
        10004 => $"Bybit rejected the request signature: the API secret does not match the key. Enter the key and secret again. [Bybit {exception.RetCode}]",
        10005 => $"The API key is missing a permission Bybit needs for this request. Check the key's read-only permissions in Bybit. [Bybit {exception.RetCode}]",
        10007 => $"Bybit could not authenticate the API key. Create a new read-only key and save it again. [Bybit {exception.RetCode}]",
        10010 => $"The API key only accepts requests from specific IP addresses. Remove the IP restriction in Bybit, or create a key without one. [Bybit {exception.RetCode}]",
        -2015 or 33004 => $"The API key has expired. Create a new read-only key and save it again. [Bybit {exception.RetCode}]",
        10006 or 10018 => RateLimited,
        131002 => $"Bybit rejected the requested date range. The next sync uses a shorter range; if it keeps failing, run a historical import. [Bybit {exception.RetCode}]",
        _ => $"Bybit rejected {context}: {exception.RetCode} - {exception.RetMsg}"
    };

    public const string RateLimited = "Bybit is limiting requests right now. Sync will try again in a minute.";

    public static string Unreachable(string endpoint) =>
        $"Could not reach Bybit ({endpoint}). Sync will try again on the next run.";
}
