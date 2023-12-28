namespace api.Shared;
public record ApiErrorResponse
{
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public ApiErrorResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
    }

    private static string? GetDefaultMessageForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad request",
            401 => "Unauthorized",
            404 => "Not found",
            500 => "Internal server error",
            _ => null
        };
    }
}
public record ApiValidationErrorResponse : ApiErrorResponse
{
    public ApiValidationErrorResponse(IEnumerable<string> errors) : base(400)
    {
        Errors = errors;
    }

    public IEnumerable<string> Errors { get; set; } = new List<string>();
}