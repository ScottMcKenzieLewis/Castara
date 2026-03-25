using Castara.Wpf.Diagnostics.CrashReport.Interfaces;

namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class CrashReportService : ICrashReportService
{
    private readonly ICrashReportBuilder _builder;
    private readonly ICrashReportWriter _writer;
    private readonly ICrashDialogService _dialogService;

    public CrashReportService(
        ICrashReportBuilder builder,
        ICrashReportWriter writer,
        ICrashDialogService dialogService)
    {
        _builder = builder;
        _writer = writer;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Handles a fatal exception by building a crash report, saving it to disk, and displaying the result to the user.
    /// If crash report generation or writing fails, displays a fallback error message.
    /// </summary>
    /// <param name="exception">The fatal exception that occurred.</param>
    /// <param name="source">The source or component where the fatal exception originated.</param>
    public void HandleFatal(Exception exception, string source)
    {
        try
        {
            var report = _builder.Build(exception, source);
            var path = _writer.Write(report);
            _dialogService.ShowCrashReportSaved(path, report.ReportId);
        }
        catch
        {
            // If crash report generation or writing fails, show a simple error dialog
            _dialogService.ShowCrashReportFailed(
                "Castara encountered an unexpected error and was unable to save a diagnostic report.");
        }
    }
}