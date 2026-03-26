namespace Castara.Api.Dtos;

/// <summary>
/// Represents a structured validation error response returned when FluentValidation detects request validation failures.
/// </summary>
/// <remarks>
/// This DTO provides a consistent structure for communicating field-level validation errors
/// to API consumers. It includes an error code, human-readable message, field-specific
/// validation failures, and a trace ID for diagnostics.
/// 
/// The structure follows API error response best practices by:
/// <list type="bullet">
/// <item><description>Providing a machine-readable error code ("validation_error")</description></item>
/// <item><description>Including a clear message for human readers</description></item>
/// <item><description>Grouping validation errors by field name for easy client-side handling</description></item>
/// <item><description>Including a trace ID for correlating with server logs</description></item>
/// </list>
/// 
/// This response format enables API consumers to:
/// <list type="bullet">
/// <item><description>Display field-specific errors next to form inputs</description></item>
/// <item><description>Programmatically handle validation failures</description></item>
/// <item><description>Report issues with trace IDs for support troubleshooting</description></item>
/// </list>
/// </remarks>
/// <example>
/// Example JSON response:
/// <code>
/// {
///   "error": "validation_error",
///   "message": "Request validation failed.",
///   "traceId": "0HMVFE3A4TQKJ:00000001",
///   "details": {
///     "Notional": ["Notional must be positive."],
///     "MaturityDate": ["MaturityDate must be after IssueDate."]
///   }
/// }
/// </code>
/// </example>
public sealed class ValidationErrorDto
{
    /// <summary>
    /// Gets or sets the error code identifying this as a validation error.
    /// </summary>
    /// <value>
    /// A machine-readable error code. Always set to "validation_error" for validation failures.
    /// </value>
    /// <remarks>
    /// This standardized error code allows API consumers to programmatically distinguish
    /// validation errors from other error types (e.g., "domain_validation_error" or "internal_server_error").
    /// </remarks>
    public string Error { get; set; } = "validation_error";

    /// <summary>
    /// Gets or sets the human-readable error message describing the failure.
    /// </summary>
    /// <value>
    /// A general message indicating that request validation failed.
    /// Defaults to "Request validation failed."
    /// </value>
    /// <remarks>
    /// This provides a high-level summary of the error. Specific field-level error messages
    /// are available in the <see cref="Details"/> dictionary.
    /// </remarks>
    public string Message { get; set; } = "Request validation failed.";

    /// <summary>
    /// Gets or sets the HTTP context trace identifier for diagnostic correlation.
    /// </summary>
    /// <value>
    /// The unique trace identifier for the request (e.g., "0HMVFE3A4TQKJ:00000001").
    /// </value>
    /// <remarks>
    /// This trace ID matches the identifier used in server logs and Application Insights,
    /// enabling correlation between client errors and server-side diagnostics.
    /// API consumers should include this ID when reporting issues to support teams.
    /// </remarks>
    /// <example>0HMVFE3A4TQKJ:00000001</example>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dictionary of field-level validation errors.
    /// </summary>
    /// <value>
    /// A dictionary where keys are property names and values are arrays of error messages
    /// for that property. Empty dictionary if no validation errors exist.
    /// </value>
    /// <remarks>
    /// This structure groups all validation errors by field name, allowing API consumers
    /// to display errors alongside the corresponding form fields. A single field can have
    /// multiple validation errors, which is why values are arrays rather than single strings.
    /// 
    /// Property names match the request DTO property names (e.g., "Notional", "MaturityDate").
    /// </remarks>
    /// <example>
    /// Example details structure:
    /// <code>
    /// {
    ///   "Notional": ["Notional must be positive.", "Notional cannot exceed 1 billion."],
    ///   "MaturityDate": ["MaturityDate must be after IssueDate."],
    ///   "Currency": ["Currency is required."]
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, string[]> Details { get; set; } = [];
}