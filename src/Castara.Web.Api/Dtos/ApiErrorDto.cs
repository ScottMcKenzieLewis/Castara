namespace Castara.Api.Dtos;

/// <summary>
/// Represents a standardized error response returned by the API.
/// </summary>
/// <remarks>
/// This DTO is used by the global exception handler to provide consistent error responses
/// across all API endpoints, including error classification, human-readable messages,
/// and trace identifiers for debugging.
/// </remarks>
public sealed class ApiErrorDto
{
    /// <summary>
    /// Gets or sets the error code that categorizes the type of error.
    /// </summary>
    /// <value>
    /// A machine-readable error code such as "validation_error" or "internal_server_error".
    /// </value>
    /// <example>validation_error</example>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable error message describing what went wrong.
    /// </summary>
    /// <value>
    /// A descriptive message providing details about the error for end users or developers.
    /// </value>
    /// <example>Unsupported currency 'JPY'.</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique trace identifier for the request.
    /// </summary>
    /// <value>
    /// The HTTP context trace identifier that can be used to correlate logs and troubleshoot issues.
    /// </value>
    /// <example>0HMVFE3A4TQKJ:00000001</example>
    public string TraceId { get; set; } = string.Empty;
}