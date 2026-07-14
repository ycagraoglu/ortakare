namespace Ortakare.Api.Common;

public class ApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }

    public static ApiResult Success(string? message = null, int statusCode = StatusCodes.Status200OK) =>
        new() { IsSuccess = true, StatusCode = statusCode, Message = message };

    public static ApiResult Failure(string message, int statusCode) =>
        new() { IsSuccess = false, StatusCode = statusCode, Message = message };
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static new ApiResult<T> Success(
        T data,
        string? message = null,
        int statusCode = StatusCodes.Status200OK) =>
        new() { IsSuccess = true, StatusCode = statusCode, Message = message, Data = data };

    public static new ApiResult<T> Failure(string message, int statusCode) =>
        new() { IsSuccess = false, StatusCode = statusCode, Message = message };
}
