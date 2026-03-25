namespace Castara.Wpf.Diagnostics.CrashReport;

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