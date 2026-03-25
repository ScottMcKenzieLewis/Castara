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


/// <summary>
/// Represents an immutable log entry captured for crash reporting.
/// </summary>
/// <param name="TimestampUtc">The UTC timestamp when the log entry was created.</param>
/// <param name="Level">The log level (e.g., Information, Warning, Error).</param>
/// <param name="Category">The log category or source.</param>
/// <param name="Message">The log message content.</param>
public sealed record CrashLogEntry(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message);

/// <summary>
/// Represents immutable exception information captured during a crash.
/// </summary>
/// <param name="Type">The fully qualified type name of the exception.</param>
/// <param name="Message">The exception message.</param>
/// <param name="StackTrace">The stack trace of the exception, or <see langword="null"/> if unavailable.</param>
public sealed record CrashExceptionInfo(
    string Type,
    string Message,
    string? StackTrace);