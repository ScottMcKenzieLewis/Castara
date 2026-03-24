namespace Castara.Wpf.Diagnostics.CrashReport.Abstractions;

public interface ICrashReportService
{
    void Handle(Exception exception, string source);
}