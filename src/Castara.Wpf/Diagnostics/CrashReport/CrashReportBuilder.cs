using Castara.Wpf.Diagnostics.CrashReport.Abstractions;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class CrashReportBuilder : ICrashReportBuilder
{
    private readonly IApplicationStateSnapshotService _snapshotService;
    private readonly IObservableLogStore _logStore;

    public CrashReportBuilder(
        IApplicationStateSnapshotService snapshotService,
        IObservableLogStore logStore)
    {
        _snapshotService = snapshotService;
        _logStore = logStore;
    }

    public CrashReport Build(Exception exception)
    {
        var snapshot = _snapshotService.GetSnapshot();
        var reportId = Guid.NewGuid().ToString("N");

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";

        var recentLogs = _logStore.Entries
            .TakeLast(200)
            .Select(x => new CrashLogEntry(
                TimestampUtc: x.Timestamp,
                Level: x.Level.ToString(),
                Category: x.Category,
                Message: Sanitize(x.Message) ?? string.Empty))
            .ToArray();

        return new CrashReport(
            ReportId: reportId,
            TimestampUtc: DateTimeOffset.UtcNow,
            ApplicationName: "Castara",
            ApplicationVersion: version,
            RuntimeVersion: Environment.Version.ToString(),
            OperatingSystem: RuntimeInformation.OSDescription,
            Theme: snapshot.Theme,
            ActiveView: snapshot.ActiveView,
            SelectedCastingProfile: snapshot.SelectedCastingProfile,
            Exception: ToInfo(exception),
            InnerExceptions: FlattenInnerExceptions(exception).ToArray(),
            Context: snapshot.Fields,
            RecentLogs: recentLogs);
    }

    private static CrashExceptionInfo ToInfo(Exception ex) =>
        new(
            Type: ex.GetType().FullName ?? ex.GetType().Name,
            Message: Sanitize(ex.Message) ?? string.Empty,
            StackTrace: Sanitize(ex.StackTrace));

    private static IEnumerable<CrashExceptionInfo> FlattenInnerExceptions(Exception ex)
    {
        var current = ex.InnerException;
        while (current is not null)
        {
            yield return ToInfo(current);
            current = current.InnerException;
        }
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = value;

        if (!string.IsNullOrWhiteSpace(Environment.UserName))
        {
            sanitized = sanitized.Replace(
                Environment.UserName,
                "[redacted-user]",
                StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
    }
}