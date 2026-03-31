using Castara.Wpf.Diagnostics.CrashReport.Interfaces;

namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public sealed class CrashReportUploadMapper : ICrashReportUploadMapper
{
    public SubmitCrashReportRequestDto Map(CrashReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new SubmitCrashReportRequestDto(
            Report: new CrashReportDto(
                ReportId: report.ReportId,
                TimestampUtc: report.TimestampUtc,
                ApplicationName: report.ApplicationName,
                ApplicationVersion: report.ApplicationVersion,
                RuntimeVersion: report.RuntimeVersion,
                OperatingSystem: report.OperatingSystem,
                Source: report.Source,
                Exception: MapException(report.Exception),
                InnerExceptions: report.InnerExceptions.Select(MapException).ToArray(),
                Context: MapContext(report.Context),
                RecentLogs: report.RecentLogs.Select(MapLog).ToArray()));
    }

    private static IReadOnlyDictionary<string, string> MapContext(
        IReadOnlyDictionary<string, string> source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return new Dictionary<string, string>(source, StringComparer.Ordinal);
    }

    private static CrashExceptionInfoDto MapException(CrashExceptionInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new CrashExceptionInfoDto(
            Type: source.Type,
            Message: source.Message,
            StackTrace: source.StackTrace);
    }

    private static CrashLogEntryDto MapLog(CrashLogEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new CrashLogEntryDto(
            TimestampUtc: source.TimestampUtc,
            Level: source.Level,
            Category: source.Category,
            Message: source.Message);
    }
}