using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Services.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/diagnostics/crash-reports")]
public sealed class CrashReportsController : ControllerBase
{
    private readonly ICrashReportStorageService _storageService;

    public CrashReportsController(ICrashReportStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost]
    [RequestSizeLimit(256 * 1024)]
    [ProducesResponseType(typeof(SubmitCrashReportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitCrashReportResponse>> SubmitAsync(
        [FromBody] SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storageService.StoreAsync(request, cancellationToken);

        return Accepted(new SubmitCrashReportResponse(
            IncidentId: result.IncidentId,
            ReceivedAtUtc: result.ReceivedAtUtc,
            Status: "accepted"));
    }
}
