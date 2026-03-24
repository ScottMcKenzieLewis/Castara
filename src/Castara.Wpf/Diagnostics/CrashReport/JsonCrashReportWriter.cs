using Castara.Wpf.Diagnostics.CrashReport.Abstractions;
using System.IO;
using System.Text.Json;

namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class JsonCrashReportWriter : ICrashReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Write(CrashReport report)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castara",
            "CrashReports");

        Directory.CreateDirectory(root);

        var fileName = $"{report.TimestampUtc:yyyyMMdd_HHmmss}_{report.ReportId}.json";
        var path = Path.Combine(root, fileName);

        var json = JsonSerializer.Serialize(report, SerializerOptions);
        File.WriteAllText(path, json);

        return path;
    }
}
