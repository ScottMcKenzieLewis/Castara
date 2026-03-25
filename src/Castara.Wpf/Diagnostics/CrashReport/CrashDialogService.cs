using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using System.Windows;

namespace Castara.Wpf.Diagnostics.CrashReport;

/// <summary>
/// Provides WPF-based dialog services for displaying crash report information to the user.
/// </summary>
public sealed class CrashDialogService : ICrashDialogService
{
    /// <summary>
    /// Displays a message box indicating that a crash report was successfully saved.
    /// </summary>
    /// <param name="filePath">The file path where the crash report was saved.</param>
    /// <param name="reportId">The unique identifier of the crash report.</param>
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

    /// <summary>
    /// Displays a message box indicating that crash report generation failed.
    /// </summary>
    /// <param name="fallbackMessage">The fallback error message to display to the user.</param>
    public void ShowCrashReportFailed(string fallbackMessage)
    {
        MessageBox.Show(
            fallbackMessage,
            "Castara Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
