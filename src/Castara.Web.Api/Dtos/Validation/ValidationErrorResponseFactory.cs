using FluentValidation.Results;

namespace Castara.Web.Api.Dtos.Validation;

/// <summary>
/// Creates standardized HTTP 400 validation error responses from FluentValidation results.
/// </summary>
/// <remarks>
/// <para>
/// Provides a centralized factory for transforming <see cref="ValidationResult"/> objects into
/// consistent, structured error responses. All validation failures across the API are formatted
/// identically, making client-side error handling predictable and reliable.
/// </para>
/// <para>
/// <strong>Response Structure:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Errors grouped by property name (e.g., "Email", "Password")</description></item>
/// <item><description>Duplicate messages automatically deduplicated per property</description></item>
/// <item><description>Correlation trace ID included for diagnostic tracking</description></item>
/// <item><description>Standard error code and message ("validation_error")</description></item>
/// </list>
/// </remarks>
public sealed class ValidationErrorResponseFactory : IValidationErrorResponseFactory
{
    /// <summary>
    /// Creates an HTTP 400 Bad Request response with structured validation error details.
    /// </summary>
    /// <param name="validationResult">The validation result containing failure information.</param>
    /// <param name="traceId">Correlation identifier for request tracking and diagnostics.</param>
    /// <returns>
    /// An <see cref="IResult"/> representing HTTP 400 with a <see cref="ValidationErrorDto"/> payload
    /// containing field-level errors grouped by property name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Processing Steps:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>Groups all validation failures by property name</description></item>
    /// <item><description>Deduplicates error messages within each property group</description></item>
    /// <item><description>Constructs <see cref="ValidationErrorDto"/> with errors and trace ID</description></item>
    /// <item><description>Returns as HTTP 400 Bad Request via <see cref="TypedResults.BadRequest{TValue}(TValue)"/></description></item>
    /// </list>
    /// <para>
    /// <strong>Example Response:</strong>
    /// </para>
    /// <code language="json">
    /// {
    ///   "error": "validation_error",
    ///   "message": "Request validation failed.",
    ///   "traceId": "0HMVFE3A4TQKJ:00000001",
    ///   "details": {
    ///     "Email": [
    ///       "Email address is required.",
    ///       "Email address must be valid."
    ///     ],
    ///     "Password": [
    ///       "Password must be at least 8 characters."
    ///     ]
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public ValidationErrorDto Create(ValidationResult validationResult, string traceId)
    {
        // Group validation errors by property name and remove duplicates
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        // Create the standardized validation error DTO
        var dto = new ValidationErrorDto
        {
            TraceId = traceId,
            Details = errors
            // Error and Message properties use their default values
        };

        return dto;
    }
}