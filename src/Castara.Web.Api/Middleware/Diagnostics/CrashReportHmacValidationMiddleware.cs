using Castara.Api.Configuration;
using Castara.Api.Services.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Castara.Api.Middleware.Diagnostics;

public sealed class CrashReportHmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CrashReportHmacValidationMiddleware> _logger;

    public CrashReportHmacValidationMiddleware(
        RequestDelegate next,
        ILogger<CrashReportHmacValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICrashReportRequestSignatureValidator signatureValidator,
        IOptions<CrashReportIngestionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ingestionOptions = options.Value;

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

        await _next(context);
    }

}