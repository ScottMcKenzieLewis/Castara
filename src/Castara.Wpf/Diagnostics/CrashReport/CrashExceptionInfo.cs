namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed record CrashExceptionInfo(
    string Type,
    string Message,
    string? StackTrace);
