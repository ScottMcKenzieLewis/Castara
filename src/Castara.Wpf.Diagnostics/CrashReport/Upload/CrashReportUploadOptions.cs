namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public sealed class CrashReportUploadOptions
{
    public const string SectionName = "CrashReportUpload";
    public bool Enabled { get; init; } = false;
    public string BaseUrl { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
    public string HmacKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
}