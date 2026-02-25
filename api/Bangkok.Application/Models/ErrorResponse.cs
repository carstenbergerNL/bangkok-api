namespace Bangkok.Application.Models;

/// <summary>
/// Single error entry for the API response errors array.
/// </summary>
public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Standardized error response model for API and exception middleware.
/// </summary>
public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorDetail>? Details { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
