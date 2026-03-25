using Castara.Web.Api.Dtos.Diagnostics.Requests;

namespace Castara.Web.Api.Services.Diagnostics;

public interface ICrashReportStorageService
{
    Task<StoreCrashReportResult> StoreAsync(
        SubmitCrashReportRequest request,
        CancellationToken cancellationToken);
}

public sealed record StoreCrashReportResult(
    string IncidentId,
    DateTimeOffset ReceivedAtUtc,
    string Status);
