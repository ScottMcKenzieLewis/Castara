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

/// <summary>
/// API controller for accepting and processing crash report submissions from client applications.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides a secure endpoint for receiving crash reports from Castara desktop applications
/// (WPF, WinForms, etc.). It implements multiple layers of security and validation to ensure only
/// authentic, well-formed crash reports are accepted and stored.
/// </para>
/// 
/// <para>
/// <b>Security layers:</b>
/// </para>
/// <list type="number">
/// <item><description><b>HMAC-SHA256 signature validation</b>: Verifies request authenticity and prevents tampering
/// (enforced by <see cref="RequireCrashReportHmacAttribute"/> and <c>CrashReportHmacValidationMiddleware</c>)</description></item>
/// <item><description><b>Request validation</b>: FluentValidation ensures all required fields are present and properly formatted</description></item>
/// <item><description><b>Server-side sanitization</b>: Defense-in-depth redaction of file paths and usernames before storage</description></item>
/// <item><description><b>Rate limiting</b>: Prevents abuse and resource exhaustion (applied via "public-api" policy)</description></item>
/// <item><description><b>Request size limits</b>: 256 KB maximum to prevent memory exhaustion</description></item>
/// </list>
/// 
/// <para>
/// <b>Request processing flow:</b>
/// </para>
/// <list type="number">
/// <item><description>Client sends POST request with HMAC signature headers and crash report JSON body</description></item>
/// <item><description>Middleware validates HMAC signature (401 if invalid)</description></item>
/// <item><description>Controller checks if ingestion is enabled (503 if disabled)</description></item>
/// <item><description>Request validation runs (400 if validation fails)</description></item>
/// <item><description>Crash report is sanitized and stored via storage service</description></item>
/// <item><description>HTTP 202 Accepted response returned with incident ID</description></item>
/// </list>
/// 
/// <para>
/// <b>Configuration:</b>
/// </para>
/// Crash report ingestion can be enabled or disabled via appsettings.json:
/// <code>
/// "CrashReportIngestion": {
///   "Enabled": true,
///   "AllowedClockSkewMinutes": 5,
///   "HmacKeys": {
///     "castara": "your-key-here"
///   }
/// }
/// </code>
/// 
/// <para>
/// <b>Dependencies:</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="ICrashReportStorageService"/>: Persists crash reports (configurable storage strategy)</description></item>
/// <item><description><see cref="IValidator{T}"/>: FluentValidation for request validation</description></item>
/// <item><description><see cref="IValidationErrorResponseFactory"/>: Creates RFC 7807 problem details for validation errors</description></item>
/// <item><description><see cref="ILogger{TCategoryName}"/>: Structured logging for monitoring and diagnostics</description></item>
/// <item><description><see cref="CrashReportIngestionOptions"/>: Configuration options for ingestion control</description></item>
/// </list>
/// 
/// <para>
/// <b>Rate limiting:</b>
/// </para>
/// This controller is protected by the "public-api" rate limiting policy (typically 30 requests per minute).
/// Rate limits help prevent abuse and ensure fair resource allocation.
/// 
/// <para>
/// <b>Request size limit:</b>
/// </para>
/// Crash report submissions are limited to 256 KB to prevent memory exhaustion. This is sufficient for:
/// <list type="bullet">
/// <item><description>Exception details with stack traces</description></item>
/// <item><description>Application state (composition values, settings)</description></item>
/// <item><description>Last 200 log entries</description></item>
/// </list>
/// 
/// <para>
/// <b>Timeout policy:</b>
/// </para>
/// Crash report requests have a 10-second timeout (RequestTimeout policy) to ensure timely responses
/// and prevent long-running operations from consuming resources.
/// </remarks>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportsController"/> class.
    /// </summary>
    /// <param name="storageService">The crash report storage service (strategy pattern for different storage backends).</param>
    /// <param name="validator">FluentValidation validator for crash report requests.</param>
    /// <param name="validationErrorResponseFactory">Factory for creating RFC 7807 validation error responses.</param>
    /// <param name="logger">Logger for recording crash report ingestion events and errors.</param>
    /// <param name="options">Configuration options for crash report ingestion (enabled/disabled, HMAC keys, etc.).</param>
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

    /// <summary>
    /// Accepts a crash report submission from a client application with HMAC authentication.
    /// </summary>
    /// <param name="request">The crash report submission request containing report metadata and diagnostic information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> containing a <see cref="SubmitCrashReportResponse"/> with the
    /// server-generated incident ID and acceptance status.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>HTTP Method:</b> POST
    /// </para>
    /// <para>
    /// <b>Route:</b> <c>/api/v1/diagnostics/crash-reports</c>
    /// </para>
    /// 
    /// <para>
    /// <b>Security requirements:</b>
    /// </para>
    /// This endpoint requires HMAC-SHA256 signature validation via three HTTP headers:
    /// <list type="bullet">
    /// <item><description><b>X-Castara-Key-Id</b>: Identifies which shared secret to use</description></item>
    /// <item><description><b>X-Castara-Timestamp</b>: ISO 8601 timestamp (UTC) for replay attack prevention</description></item>
    /// <item><description><b>X-Castara-Signature</b>: Hex-encoded HMAC-SHA256 of "timestamp\nbody"</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Request validation:</b>
    /// </para>
    /// The request is validated using FluentValidation to ensure:
    /// <list type="bullet">
    /// <item><description>Report ID is present and not empty</description></item>
    /// <item><description>Timestamps are valid DateTimeOffset values</description></item>
    /// <item><description>All required metadata fields are present (app name, version, OS, etc.)</description></item>
    /// <item><description>Exception information is complete</description></item>
    /// <item><description>Log entries have valid structure</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Storage:</b>
    /// </para>
    /// Crash reports are stored asynchronously via <see cref="ICrashReportStorageService"/>, which uses
    /// the strategy pattern to support different storage backends (JSON files, databases, cloud storage).
    /// The default implementation is <c>NullCrashReportStorageService</c> (discards reports), which should
    /// be replaced with a concrete implementation in production.
    /// 
    /// <para>
    /// <b>Response codes:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>HTTP 202 Accepted</b>: Crash report accepted and queued for processing</description></item>
    /// <item><description><b>HTTP 400 Bad Request</b>: Request validation failed (missing required fields, invalid format)</description></item>
    /// <item><description><b>HTTP 401 Unauthorized</b>: HMAC signature validation failed</description></item>
    /// <item><description><b>HTTP 503 Service Unavailable</b>: Crash report ingestion is disabled in configuration</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Attributes applied:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>RequestTimeout("CrashReportIngest")</b>: 10-second timeout for request processing</description></item>
    /// <item><description><b>RequestSizeLimit(256 * 1024)</b>: Maximum 256 KB request body size</description></item>
    /// <item><description><b>RequireCrashReportHmac</b>: Triggers HMAC validation middleware</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Example request:</b>
    /// </para>
    /// <code>
    /// POST /api/v1/diagnostics/crash-reports HTTP/1.1
    /// Host: api.example.com
    /// Content-Type: application/json
    /// X-Castara-Key-Id: castara-wpf-v1
    /// X-Castara-Timestamp: 2026-03-10T15:30:45.1234567Z
    /// X-Castara-Signature: A1B2C3D4E5F6...
    /// 
    /// {
    ///   "report": {
    ///     "reportId": "abc123",
    ///     "source": "DispatcherUnhandledException",
    ///     "timestampUtc": "2026-03-10T15:30:45.1234567Z",
    ///     "applicationName": "Castara",
    ///     "applicationVersion": "1.0.0.0",
    ///     "runtimeVersion": "8.0.25",
    ///     "operatingSystem": "Microsoft Windows 10.0.19045",
    ///     "exception": {
    ///       "type": "System.InvalidOperationException",
    ///       "message": "Error at [redacted-path]\\ShellViewModel.cs",
    ///       "stackTrace": "at Castara.Wpf.ViewModels.ShellViewModel..."
    ///     },
    ///     "innerExceptions": [],
    ///     "context": {
    ///       "Theme": "Light",
    ///       "ActiveView": "CalculationsViewModel"
    ///     },
    ///     "recentLogs": []
    ///   }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Example response (HTTP 202 Accepted):</b>
    /// </para>
    /// <code>
    /// {
    ///   "incidentId": "cr_20260310_153045_01HN3KQVMQXYZ5N8J7G2P4W6ST",
    ///   "receivedAtUtc": "2026-03-10T15:30:45.5678901Z",
    ///   "status": "accepted"
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Example cURL command:</b>
    /// </para>
    /// <code>
    /// TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.%NZ")
    /// PAYLOAD='{"report":{...}}'
    /// SIGNATURE=$(echo -n "$TIMESTAMP"$'\n'"$PAYLOAD" | openssl dgst -sha256 -hmac "your-secret-key" | awk '{print $2}' | tr '[:lower:]' '[:upper:]')
    /// 
    /// curl -X POST https://api.example.com/api/v1/diagnostics/crash-reports \
    ///   -H "Content-Type: application/json" \
    ///   -H "X-Castara-Key-Id: castara-wpf-v1" \
    ///   -H "X-Castara-Timestamp: $TIMESTAMP" \
    ///   -H "X-Castara-Signature: $SIGNATURE" \
    ///   -d "$PAYLOAD"
    /// </code>
    /// 
    /// <para>
    /// <b>Error scenarios:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Missing HMAC headers</b>: Returns 401 Unauthorized before reaching this action</description></item>
    /// <item><description><b>Invalid signature</b>: Returns 401 Unauthorized before reaching this action</description></item>
    /// <item><description><b>Expired timestamp</b>: Returns 401 Unauthorized (replay attack prevention)</description></item>
    /// <item><description><b>Ingestion disabled</b>: Returns 503 Service Unavailable</description></item>
    /// <item><description><b>Validation errors</b>: Returns 400 Bad Request with RFC 7807 problem details</description></item>
    /// <item><description><b>Storage failure</b>: Logged but still returns 202 (graceful degradation)</description></item>
    /// </list>
    /// </remarks>
    [RequestTimeout("CrashReportIngest")]
    [HttpPost]
    [RequestSizeLimit(256 * 1024)]
    [ProducesResponseType(typeof(SubmitCrashReportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [RequireCrashReportHmac]
    public async Task<ActionResult<SubmitCrashReportResponse>> SubmitAsync(
        [FromBody] SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        // Check if crash report ingestion is enabled in configuration
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

        // Validate request structure and required fields using FluentValidation
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Validation failed for submit crash report request. TraceId: {TraceId}",
                HttpContext.TraceIdentifier);
            return _validationErrorResponseFactory.Create(validationResult, HttpContext.TraceIdentifier);
        }

        // Store crash report asynchronously (sanitization happens in storage service)
        var result = await _storageService.StoreAsync(request, cancellationToken);

        // Return HTTP 202 Accepted with incident ID for tracking
        return Accepted(new SubmitCrashReportResponse(
            IncidentId: result.IncidentId,
            ReceivedAtUtc: result.ReceivedAtUtc,
            Status: "accepted"));
    }
}
