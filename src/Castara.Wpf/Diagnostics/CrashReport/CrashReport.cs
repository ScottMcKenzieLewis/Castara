namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed record CrashReport(
    string ReportId,
    DateTimeOffset TimestampUtc,
    string ApplicationName,
    string ApplicationVersion,
    string RuntimeVersion,
    string OperatingSystem,
    string? Theme,
    string? ActiveView,
    string? SelectedCastingProfile,
    CrashExceptionInfo Exception,
    IReadOnlyList<CrashExceptionInfo> InnerExceptions,
    IReadOnlyDictionary<string, string> Context,
    IReadOnlyList<CrashLogEntry> RecentLogs);