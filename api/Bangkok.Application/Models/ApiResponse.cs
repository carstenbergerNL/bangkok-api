namespace Bangkok.Application.Models;

/// <summary>
/// Standardized API response wrapper for all endpoints.
/// JSON: success, message, data, errors, correlationId.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public ErrorResponse? Error { get; set; }
    public List<ApiError>? Errors { get; set; }
    public string? CorrelationId { get; set; }

    public static ApiResponse<T> Ok(T data, string? correlationId = null, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data,
        CorrelationId = correlationId
    };

    public static ApiResponse<T> Fail(ErrorResponse error, string? correlationId = null) => new()
    {
        Success = false,
        Message = error.Message,
        Error = error,
        Errors = new List<ApiError> { new() { Code = error.Code, Message = error.Message } },
        CorrelationId = correlationId
    };
}
