using Castara.Wpf.Diagnostics.CrashReport.Abstractions;

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

    public void Handle(Exception exception, string source)
    {
        try
        {
            var report = _builder.Build(new CrashReportSourceException(source, exception));
            var path = _writer.Write(report);
            _dialogService.ShowCrashReportSaved(path, report.ReportId);
        }
        catch
        {
            _dialogService.ShowCrashReportFailed(
                "Castara encountered an unexpected error and was unable to save a diagnostic report.");
        }
    }
}

public sealed class CrashReportSourceException : Exception
{
    public CrashReportSourceException(string source, Exception innerException)
        : base($"Unhandled exception captured from source '{source}'.", innerException)
    {
    }
}