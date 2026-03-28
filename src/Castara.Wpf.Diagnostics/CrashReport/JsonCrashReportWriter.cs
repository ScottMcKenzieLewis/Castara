using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using System.IO;
using System.Text.Json;

namespace Castara.Wpf.Diagnostics.CrashReport;

/// <summary>
/// Writes crash reports to JSON files in the local application data directory.
/// Reports are stored in %LocalAppData%\Castara\CrashReports with timestamped filenames.
/// </summary>
public sealed class JsonCrashReportWriter : ICrashReportWriter
{
    /// <summary>
    /// JSON serializer options configured for human-readable indented output.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Writes a crash report to a JSON file in the application's crash reports directory.
    /// </summary>
    /// <param name="report">The crash report to write.</param>
    /// <returns>The full path to the saved crash report file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory path is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to write to the directory.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs during file writing.</exception>
    public string Write(CrashReport report)
    {
        // Create the crash reports directory in the user's local app data folder
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castara",
            "CrashReports");

        Directory.CreateDirectory(root);

        // Generate a timestamped filename with the report ID: yyyyMMdd_HHmmss_reportId.json
        var fileName = $"{report.TimestampUtc:yyyyMMdd_HHmmss}_{report.ReportId}.json";
        var path = Path.Combine(root, fileName);

        // Serialize the crash report to JSON and write to file
        var json = JsonSerializer.Serialize(report, SerializerOptions);
        File.WriteAllText(path, json);

        return path;
    }
}
