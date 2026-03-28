using System.Text.Json;

namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public static class CrashReportUploadJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }
}