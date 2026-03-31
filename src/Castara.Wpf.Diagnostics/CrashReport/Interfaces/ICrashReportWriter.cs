namespace Castara.Wpf.Diagnostics.CrashReport.Interfaces;

/// <summary>
/// Defines a contract for writing crash reports to a specific format or destination.
/// </summary>
public interface ICrashReportWriter
{
    /// <summary>
    /// Writes the specified crash report and returns the formatted output.
    /// </summary>
    /// <param name="report">The crash report to write.</param>
    /// <returns>The formatted crash report as a string.</returns>
    string Write(CrashReport report);
}
