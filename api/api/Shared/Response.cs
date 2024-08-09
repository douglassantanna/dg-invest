namespace api.Shared;
public record Response(string Message, bool IsSuccess, object? Data = null);