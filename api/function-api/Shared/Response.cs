
namespace function_api.Shared;
public record Response(string Message, bool IsSuccess, object Data = null);

// public interface IResponseService
// {
//     Response Success(string message, object data = null);
//     Response Error(string message, object data = null);
// };