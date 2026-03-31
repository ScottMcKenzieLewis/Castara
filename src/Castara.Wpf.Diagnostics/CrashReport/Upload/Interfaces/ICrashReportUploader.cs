namespace Castara.Wpf.Diagnostics.CrashReport.Upload.Interfaces;

public interface ICrashReportUploader
{
    Task<CrashReportUploadResult> UploadAsync(
        CrashReport report,
        CancellationToken cancellationToken);
}