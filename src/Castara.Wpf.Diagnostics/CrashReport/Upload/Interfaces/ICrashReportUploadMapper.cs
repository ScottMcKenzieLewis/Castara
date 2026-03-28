using Castara.Wpf.Diagnostics.CrashReport;

namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public interface ICrashReportUploadMapper
{
    SubmitCrashReportRequestDto Map(CrashReport report);
}