namespace Castara.Wpf.Diagnostics.CrashReport.Abstractions;

public interface ICrashReportWriter
{
    string Write(CrashReport report);
}
