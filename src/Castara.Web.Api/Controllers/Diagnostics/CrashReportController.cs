using Castara.Api.Configuration;
using Castara.Api.Dtos;
using Castara.Api.Services.Diagnostics;
using Castara.Web.Api.Attributes.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Services.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/v1/diagnostics/crash-reports")]
public sealed class CrashReportsController : ControllerBase
{
    private const string ApiKeyHeaderName = "X-Castara-Api-Key";

    private readonly ICrashReportStorageService _storageService;
    private readonly IValidator<SubmitCrashReportRequest> _validator;
    private readonly IValidationErrorResponseFactory _validationErrorResponseFactory;
    private readonly ILogger<CrashReportsController> _logger;
    private readonly CrashReportIngestionOptions _options;

    public CrashReportsController(ICrashReportStorageService storageService,
        IValidator<SubmitCrashReportRequest> validator,
        IValidationErrorResponseFactory validationErrorResponseFactory,
        ILogger<CrashReportsController> logger,
        IOptions<CrashReportIngestionOptions> options)
    {
        _storageService = storageService;
        _validator = validator;
        _validationErrorResponseFactory = validationErrorResponseFactory;
        _logger = logger;
        _options = options.Value;
    }

    [RequestTimeout("CrashReportIngest")]
    [HttpPost]
    [RequestSizeLimit(256 * 1024)]
    [ProducesResponseType(typeof(SubmitCrashReportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequireCrashReportHmac]
    public async Task<ActionResult<SubmitCrashReportResponse>> SubmitAsync(
        [FromBody] SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {

        if (!_options.Enabled)
        {
            _logger.LogWarning("Crash report ingestion request rejected because ingestion is disabled.");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Crash report ingestion is disabled.",
                    Detail = "The service is not currently accepting crash reports.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }

        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Validation failed for bond valuation request. TraceId: {TraceId}",
                HttpContext.TraceIdentifier);
            return _validationErrorResponseFactory.Create(validationResult, HttpContext.TraceIdentifier);
        }

        var result = await _storageService.StoreAsync(request, cancellationToken);

        return Accepted(new SubmitCrashReportResponse(
            IncidentId: result.IncidentId,
            ReceivedAtUtc: result.ReceivedAtUtc,
            Status: "accepted"));
    }
}
