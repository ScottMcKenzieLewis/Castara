namespace Castara.Wpf.Diagnostics.CrashReport.Abstractions;

public interface ICrashDialogService
{
    void ShowCrashReportSaved(string filePath, string reportId);
    void ShowCrashReportFailed(string fallbackMessage);
}