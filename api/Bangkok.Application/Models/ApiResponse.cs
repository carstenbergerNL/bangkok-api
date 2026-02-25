namespace Bangkok.Application.Models;

/// <summary>
/// Standardized API response wrapper for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ErrorResponse? Error { get; set; }
    public string? CorrelationId { get; set; }

    public static ApiResponse<T> Ok(T data, string? correlationId = null) => new()
    {
        Success = true,
        Data = data,
        CorrelationId = correlationId
    };

    public static ApiResponse<T> Fail(ErrorResponse error, string? correlationId = null) => new()
    {
        Success = false,
        Error = error,
        CorrelationId = correlationId
    };
}
