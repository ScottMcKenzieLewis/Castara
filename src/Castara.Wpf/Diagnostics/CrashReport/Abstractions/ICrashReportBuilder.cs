namespace Castara.Wpf.Diagnostics.CrashReport.Abstractions;

public interface ICrashReportBuilder
{
    CrashReport Build(Exception exception);
}
