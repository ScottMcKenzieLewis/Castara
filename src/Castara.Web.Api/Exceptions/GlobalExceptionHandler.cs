using System.Text.Json;
using Castara.Api.Dtos;
using Microsoft.AspNetCore.Diagnostics;

namespace Castara.Api.Exceptions;

/// <summary>
/// Global exception handler that intercepts and processes unhandled exceptions throughout the application.
/// </summary>
/// <remarks>
/// This handler provides consistent error responses across the API by:
/// <list type="bullet">
/// <item><description>Mapping exceptions to appropriate HTTP status codes</description></item>
/// <item><description>Distinguishing between domain validation errors and general validation errors</description></item>
/// <item><description>Logging exceptions with contextual information</description></item>
/// <item><description>Returning standardized JSON error responses</description></item>
/// </list>
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for recording exception details.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to handle an exception by mapping it to an appropriate HTTP response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}"/> that represents the asynchronous operation.
    /// Returns <c>true</c> if the exception was handled; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Exception handling logic:
    /// <list type="table">
    /// <listheader>
    /// <term>Exception Type</term>
    /// <description>Response</description>
    /// </listheader>
    /// <item>
    /// <term><see cref="ArgumentException"/></term>
    /// <description>400 Bad Request with "validation_error" code for input validation failures</description>
    /// </item>
    /// <item>
    /// <term>Other exceptions</term>
    /// <description>500 Internal Server Error with generic error message</description>
    /// </item>
    /// </list>
    /// If the response has already started streaming, the exception cannot be handled and returns <c>false</c>.
    /// </remarks>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error, message, level) = exception switch
        {           
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "validation_error",
                exception.Message,
                LogLevel.Warning),

            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                "An unexpected error occurred.",
                LogLevel.Error)
        };

        _logger.Log(
            level,
            exception,
            "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var dto = new ApiErrorDto
        {
            Error = error,
            Message = message,
            TraceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(
                dto,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
            cancellationToken);

        return true;
    }
}