using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Castara.Web.Api.Dtos.Validation;

/// <summary>
/// Defines a factory for creating standardized validation error responses.
/// </summary>
/// <remarks>
/// This interface abstracts the creation of HTTP 400 Bad Request responses
/// from FluentValidation validation failures. It ensures consistent error
/// response formatting across all API endpoints that use FluentValidation,
/// making it easier for API consumers to parse and handle validation errors.
/// 
/// The factory pattern is used to:
/// <list type="bullet">
/// <item><description>Centralize validation error response formatting logic</description></item>
/// <item><description>Ensure consistent structure for all validation failures</description></item>
/// <item><description>Simplify controller code by extracting response creation</description></item>
/// <item><description>Enable easy modification of error response format in one place</description></item>
/// </list>
/// </remarks>
public interface IValidationErrorResponseFactory
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
    /// The returned response includes:
    /// <list type="bullet">
    /// <item><description>An error code identifying it as a validation error</description></item>
    /// <item><description>A human-readable error message</description></item>
    /// <item><description>Detailed field-level validation failures grouped by property name</description></item>
    /// <item><description>A trace ID for troubleshooting and log correlation</description></item>
    /// </list>
    /// This method should be called in controllers after FluentValidation detects validation failures
    /// to return a standardized error response to the client.
    /// </remarks>
    ValidationErrorDto Create(ValidationResult validationResult, string traceId);
}