using Castara.Wpf.Diagnostics.CrashReport.Abstractions;
using System.Windows;

namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class CrashDialogService : ICrashDialogService
{
    public void ShowCrashReportSaved(string filePath, string reportId)
    {
        MessageBox.Show(
            $"Castara encountered an unexpected error.\n\n" +
            $"A diagnostic report was saved.\n\n" +
            $"Report ID: {reportId}\n" +
            $"Location: {filePath}",
            "Castara Error Report",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public void ShowCrashReportFailed(string fallbackMessage)
    {
        MessageBox.Show(
            fallbackMessage,
            "Castara Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
