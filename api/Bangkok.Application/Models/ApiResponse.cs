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

    /// <summary>
    /// Single error: use Error only; Message and Errors null to avoid duplicating error text.
    /// </summary>
    public static ApiResponse<T> Fail(ErrorResponse error, string? correlationId = null) => new()
    {
        Success = false,
        Message = null,
        Data = default,
        Error = error,
        Errors = null,
        CorrelationId = correlationId
    };

    /// <summary>
    /// Multiple errors (e.g. validation): use Errors array; Error set to first for backward compatibility.
    /// </summary>
    public static ApiResponse<T> Fail(IReadOnlyList<ApiError> errors, string? correlationId = null)
    {
        var first = errors.Count > 0 ? errors[0] : new ApiError();
        return new ApiResponse<T>
        {
            Success = false,
            Message = first.Message,
            Data = default,
            Error = new ErrorResponse { Code = first.Code, Message = first.Message },
            Errors = errors.ToList(),
            CorrelationId = correlationId
        };
    }
}
