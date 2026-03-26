using Castara.Api.Diagnostics.Services;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Services.Diagnostics;
using NUlid;

namespace Castara.Diagnostics.Api.Services.Diagnostics;

/// <summary>
/// A Null Object Pattern implementation of <see cref="ICrashReportStorageService"/> that accepts
/// crash reports but does not persist them to any storage backend.
/// </summary>
/// <remarks>
/// <para>
/// This service is typically used when crash report ingestion is disabled, during testing,
/// or in development environments where actual storage is not needed. Despite not storing
/// the reports, this service still:
/// </para>
/// <list type="bullet">
/// <item><description>Sanitizes the incoming request to validate the sanitization logic</description></item>
/// <item><description>Generates unique incident IDs for correlation and logging</description></item>
/// <item><description>Returns a valid result with "accepted-noop" status</description></item>
/// </list>
/// <para>
/// This implementation completes synchronously and returns immediately with minimal overhead.
/// </para>
/// </remarks>
public sealed class NullCrashReportStorageService : ICrashReportStorageService
{
    private readonly ICrashReportSanitizer _sanitizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullCrashReportStorageService"/> class.
    /// </summary>
    /// <param name="sanitizer">The crash report sanitizer for validating sanitization logic.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sanitizer"/> is <see langword="null"/>.</exception>
    public NullCrashReportStorageService(ICrashReportSanitizer sanitizer)
    {
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    /// <summary>
    /// Accepts a crash report, sanitizes it for validation purposes, generates an incident ID,
    /// but does not persist the report to any storage backend.
    /// </summary>
    /// <param name="request">The crash report submission request to accept (but not store).</param>
    /// <param name="cancellationToken">Cancellation token (not used in this implementation).</param>
    /// <returns>
    /// A task that represents the synchronous operation. The task result contains a
    /// <see cref="StoreCrashReportResult"/> with status "accepted-noop" indicating the report
    /// was received but not stored.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Even though this service does not store reports, it still sanitizes the request to ensure
    /// the sanitization logic is executed and validated as part of the request pipeline (defense-in-depth).
    /// </para>
    /// <para>
    /// The incident ID format is: <c>cr_yyyyMMdd_HHmmss_&lt;ULID&gt;</c>, which provides a
    /// timestamp-prefixed, globally unique identifier suitable for correlation and logging.
    /// </para>
    /// <para>
    /// This service completes synchronously and returns immediately without performing any I/O operations.
    /// It is ideal for development environments, testing scenarios, or when crash report ingestion is
    /// intentionally disabled.
    /// </para>
    /// </remarks>
    public Task<StoreCrashReportResult> StoreAsync(
        SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Still sanitize the request to validate sanitization logic (defense-in-depth)
        var sanitizedRequest = _sanitizer.Sanitize(request);

        // Generate a unique incident ID with timestamp and ULID: cr_yyyyMMdd_HHmmss_<ulid>
        var incidentId = $"cr_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Ulid.NewUlid()}";

        // Return result indicating the report was accepted but not stored
        var result = new StoreCrashReportResult(
            IncidentId: incidentId,
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            Status: "accepted-noop");

        return Task.FromResult(result);
    }
}
