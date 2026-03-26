namespace Castara.Web.Api.Dtos.Diagnostics;

/// <summary>
/// Represents a comprehensive crash report containing exception details, system information, 
/// application state, and diagnostic logs submitted from a client application.
/// </summary>
/// <param name="ReportId">Unique identifier for this crash report (GUID without hyphens).</param>
/// <param name="TimestampUtc">UTC timestamp when the crash occurred.</param>
/// <param name="ApplicationName">Name of the application that crashed (e.g., "Castara").</param>
/// <param name="ApplicationVersion">Version of the application that crashed (e.g., "1.0.0.0").</param>
/// <param name="RuntimeVersion">The .NET runtime version (e.g., "8.0.25").</param>
/// <param name="OperatingSystem">Operating system description (e.g., "Microsoft Windows 10.0.19045").</param>
/// <param name="Source">The source or origin of the crash (e.g., "DispatcherUnhandledException", "TaskSchedulerUnobservedException").</param>
/// <param name="Exception">Primary exception information that caused the crash.</param>
/// <param name="InnerExceptions">Collection of inner exceptions, if any, flattened from the exception hierarchy.</param>
/// <param name="Context">Dictionary of application state key-value pairs at the time of the crash (e.g., theme, active view, user inputs).</param>
/// <param name="RecentLogs">Collection of recent log entries leading up to the crash (typically last 200 entries).</param>
/// <remarks>
/// All file paths and usernames in exception messages, stack traces, context values, and logs are 
/// sanitized by the client before submission to protect user privacy while preserving filenames for debugging.
/// </remarks>
public sealed record CrashReportDto(
    string ReportId,
    DateTimeOffset TimestampUtc,
    string ApplicationName,
    string ApplicationVersion,
    string RuntimeVersion,
    string OperatingSystem,
    string Source,
    CrashExceptionInfoDto Exception,
    IReadOnlyList<CrashExceptionInfoDto> InnerExceptions,
    IReadOnlyDictionary<string, string> Context,
    IReadOnlyList<CrashLogEntryDto> RecentLogs);

/// <summary>
/// Represents exception information captured during a crash, including type, message, and stack trace.
/// </summary>
/// <param name="Type">Fully qualified type name of the exception (e.g., "System.InvalidOperationException").</param>
/// <param name="Message">The exception message. File paths and usernames are sanitized to "[redacted-path]\filename" format.</param>
/// <param name="StackTrace">The exception stack trace, or <see langword="null"/> if unavailable. File paths are sanitized for privacy.</param>
public sealed record CrashExceptionInfoDto(
    string Type,
    string Message,
    string? StackTrace);

/// <summary>
/// Represents a single log entry captured in the application's diagnostic log system.
/// </summary>
/// <param name="TimestampUtc">UTC timestamp when the log entry was created.</param>
/// <param name="Level">Log level (e.g., "Trace", "Debug", "Information", "Warning", "Error", "Critical").</param>
/// <param name="Category">Log category or source (e.g., "Castara.Wpf.ViewModels.ShellViewModel").</param>
/// <param name="Message">The log message content. File paths and usernames are sanitized for privacy.</param>
public sealed record CrashLogEntryDto(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message);
