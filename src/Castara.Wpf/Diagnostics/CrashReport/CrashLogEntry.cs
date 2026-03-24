namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed record CrashLogEntry(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message);