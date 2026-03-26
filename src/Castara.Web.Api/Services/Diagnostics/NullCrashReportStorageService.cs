using Castara.Api.Diagnostics.Services;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Services.Diagnostics;
using NUlid;

namespace Castara.Diagnostics.Api.Services.Diagnostics;

public sealed class NullCrashReportStorageService : ICrashReportStorageService
{

    private readonly ICrashReportSanitizer _sanitizer;

    public NullCrashReportStorageService(ICrashReportSanitizer sanitizer)
    {
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    public Task<StoreCrashReportResult> StoreAsync(
        SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sanitizedRequest = _sanitizer.Sanitize(request);

        var incidentId = $"cr_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Ulid.NewUlid()}";

        var result = new StoreCrashReportResult(
            IncidentId: incidentId,
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            Status: "accepted-noop");

        return Task.FromResult(result);
    }
}
