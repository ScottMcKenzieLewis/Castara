using Castara.Api.Configuration;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Dtos.Validation;
using Castara.Web.Api.Services.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;

namespace Castara.Web.Api.Endpoints.Diagnostics;

public static class CrashReportEndpoints
{
    [RequestTimeout("CrashReportIngest")]
    public static async Task<IResult> SubmitAsync(
        SubmitCrashReportRequest request,
        ICrashReportStorageService storageService,
        IValidator<SubmitCrashReportRequest> validator,
        IValidationErrorResponseFactory validationErrorResponseFactory,
        ILoggerFactory loggerFactory,
        IOptions<CrashReportIngestionOptions> options,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CrashReportEndpoints");

        if (!options.Value.Enabled)
        {
            logger.LogWarning("Crash report ingestion request rejected because ingestion is disabled.");

            return TypedResults.Problem(
                title: "Crash report ingestion is disabled.",
                detail: "The service is not currently accepting crash reports.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "Validation failed for submit crash report request. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            return TypedResults.BadRequest(validationErrorResponseFactory.Create(
                validationResult,
                httpContext.TraceIdentifier));
        }

        var result = await storageService.StoreAsync(request, cancellationToken);

        return TypedResults.Accepted(
            uri: (string?)null,
            value: new SubmitCrashReportResponse(
                IncidentId: result.IncidentId,
                ReceivedAtUtc: result.ReceivedAtUtc,
                Status: "accepted"));
    }
}