using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Services.Diagnostics;
using NUlid;

namespace Castara.Diagnostics.Api.Services.Diagnostics;

public sealed class NullCrashReportStorageService : ICrashReportStorageService
{
    public Task<StoreCrashReportResult> StoreAsync(
        SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var incidentId = $"cr_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Ulid.NewUlid()}";

        var result = new StoreCrashReportResult(
            IncidentId: incidentId,
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            Status: "accepted-noop");

        return Task.FromResult(result);
    }
}
