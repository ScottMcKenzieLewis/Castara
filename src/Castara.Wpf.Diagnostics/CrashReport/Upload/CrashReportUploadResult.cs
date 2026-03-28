public sealed record CrashReportUploadResult(
    bool Success,
    string? IncidentId,
    string? Status,
    string? ErrorMessage);