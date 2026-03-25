namespace Castara.Wpf.Diagnostics.CrashReport;

/// <summary>
/// Represents an immutable, comprehensive crash report containing exception details, system information, and diagnostic data.
/// </summary>
/// <param name="ReportId">The unique identifier for this crash report.</param>
/// <param name="Source">The source or origin of the crash (e.g., module or component name).</param>
/// <param name="TimestampUtc">The UTC timestamp when the crash occurred.</param>
/// <param name="ApplicationName">The name of the application that crashed.</param>
/// <param name="ApplicationVersion">The version of the application that crashed.</param>
/// <param name="RuntimeVersion">The .NET runtime version.</param>
/// <param name="OperatingSystem">The operating system information.</param>
/// <param name="Exception">The primary exception that caused the crash.</param>
/// <param name="InnerExceptions">A collection of inner exceptions, if any.</param>
/// <param name="Context">A dictionary containing additional contextual information and application state.</param>
/// <param name="RecentLogs">A collection of recent log entries captured before the crash.</param>
public sealed record CrashReport(
    string ReportId,
    string Source,
    DateTimeOffset TimestampUtc,
    string ApplicationName,
    string ApplicationVersion,
    string RuntimeVersion,
    string OperatingSystem,
    CrashExceptionInfo Exception,
    IReadOnlyList<CrashExceptionInfo> InnerExceptions,
    IReadOnlyDictionary<string, string> Context,
    IReadOnlyList<CrashLogEntry> RecentLogs);