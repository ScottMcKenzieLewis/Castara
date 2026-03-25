namespace Castara.Wpf.Diagnostics.CrashReport;

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
