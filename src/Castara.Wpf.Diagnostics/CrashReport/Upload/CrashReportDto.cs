public sealed record SubmitCrashReportRequestDto(
    CrashReportDto Report);

public sealed record SubmitCrashReportResponseDto(
    string IncidentId,
    DateTimeOffset ReceivedAtUtc,
    string Status);

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

public sealed record CrashExceptionInfoDto(
    string Type,
    string Message,
    string? StackTrace);

public sealed record CrashLogEntryDto(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    string Message);