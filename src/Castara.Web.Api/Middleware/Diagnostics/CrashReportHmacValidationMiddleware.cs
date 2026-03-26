using Castara.Api.Configuration;
using Castara.Api.Services.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Castara.Api.Middleware.Diagnostics;

/// <summary>
/// ASP.NET Core middleware that validates HMAC signatures on crash report submission requests
/// to ensure authenticity and prevent unauthorized report ingestion.
/// </summary>
/// <remarks>
/// This middleware performs two validations:
/// <list type="number">
/// <item>Checks if crash report ingestion is enabled (returns 503 if disabled)</item>
/// <item>Validates the HMAC signature of the request (returns 401 if invalid)</item>
/// </list>
/// If both validations pass, the request proceeds to the next middleware in the pipeline.
/// </remarks>
public sealed class CrashReportHmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CrashReportHmacValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportHmacValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the request pipeline.</param>
    /// <param name="logger">Logger for recording validation failures and security events.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="next"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public CrashReportHmacValidationMiddleware(
        RequestDelegate next,
        ILogger<CrashReportHmacValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to validate crash report ingestion status and HMAC signature.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="signatureValidator">Service for validating HMAC signatures on crash report requests.</param>
    /// <param name="options">Configuration options for crash report ingestion, including enabled/disabled state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Validation Steps:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Service Availability:</term>
    /// <description>If ingestion is disabled, returns HTTP 503 (Service Unavailable) with a problem details response.</description>
    /// </item>
    /// <item>
    /// <term>HMAC Validation:</term>
    /// <description>If the request signature is invalid, returns HTTP 401 (Unauthorized) and logs a warning.</description>
    /// </item>
    /// <item>
    /// <term>Success:</term>
    /// <description>If both validations pass, invokes the next middleware in the pipeline.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public async Task InvokeAsync(
        HttpContext context,
        ICrashReportRequestSignatureValidator signatureValidator,
        IOptions<CrashReportIngestionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ingestionOptions = options.Value;

        // Check if crash report ingestion is enabled in configuration
        if (!ingestionOptions.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Crash report ingestion is disabled.",
                Detail = "The service is not currently accepting crash reports.",
                Status = StatusCodes.Status503ServiceUnavailable
            });

            return;
        }

        // Validate HMAC signature to ensure request authenticity
        var signatureValid = await signatureValidator.IsValidAsync(
            context.Request,
            context.RequestAborted);

        if (!signatureValid)
        {
            _logger.LogWarning("Crash report rejected due to invalid HMAC signature.");

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid request signature is required.",
                Status = StatusCodes.Status401Unauthorized
            });

            return;
        }

        // Validation passed, proceed to next middleware
        await _next(context);
    }

}