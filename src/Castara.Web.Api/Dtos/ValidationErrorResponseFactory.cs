using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Castara.Api.Dtos.Responses;

/// <summary>
/// Factory for creating standardized validation error responses from FluentValidation results.
/// </summary>
/// <remarks>
/// This factory transforms FluentValidation's <see cref="ValidationResult"/> objects into
/// consistent HTTP 400 Bad Request responses with a <see cref="ValidationErrorDto"/> body.
/// It consolidates validation error creation logic in one place, ensuring all API endpoints
/// return validation errors in the same structured format.
/// 
/// The factory:
/// <list type="bullet">
/// <item><description>Groups validation errors by property name for easy client-side field mapping</description></item>
/// <item><description>Removes duplicate error messages for the same property</description></item>
/// <item><description>Includes trace IDs for diagnostic correlation</description></item>
/// <item><description>Returns HTTP 400 Bad Request with structured error details</description></item>
/// </list>
/// 
/// This centralized approach makes it easier to modify error response formatting across
/// the entire API by updating only this factory implementation.
/// </remarks>
public sealed class ValidationErrorResponseFactory : IValidationErrorResponseFactory
{
    /// <summary>
    /// Creates a BadRequestObjectResult containing structured validation error information.
    /// </summary>
    /// <param name="validationResult">
    /// The FluentValidation validation result containing the validation failures.
    /// </param>
    /// <param name="traceId">
    /// The HTTP context trace identifier for correlating the error with logs and diagnostics.
    /// </param>
    /// <returns>
    /// A <see cref="BadRequestObjectResult"/> (HTTP 400) containing a <see cref="ValidationErrorDto"/>
    /// with field-level validation errors grouped by property name.
    /// </returns>
    /// <remarks>
    /// The method performs the following transformations:
    /// <list type="number">
    /// <item><description>Extracts all validation errors from the FluentValidation result</description></item>
    /// <item><description>Groups errors by property name (e.g., "Notional", "MaturityDate")</description></item>
    /// <item><description>Removes duplicate error messages for each property using Distinct()</description></item>
    /// <item><description>Converts to a dictionary for easy JSON serialization</description></item>
    /// <item><description>Wraps in a ValidationErrorDto with trace ID and standard error code</description></item>
    /// <item><description>Returns as HTTP 400 Bad Request with the DTO as the response body</description></item>
    /// </list>
    /// 
    /// Example output for a bond valuation request with negative notional and invalid dates:
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
    /// </remarks>
    public BadRequestObjectResult Create(ValidationResult validationResult, string traceId)
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

        // Return HTTP 400 Bad Request with the error DTO
        return new BadRequestObjectResult(dto);
    }
}