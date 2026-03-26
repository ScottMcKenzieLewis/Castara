namespace Castara.Api.Configuration;

public sealed class CrashReportIngestionOptions
{

    public const string SectionName = "CrashReportIngestion";
    public bool Enabled { get; init; } = true;
    public int AllowedClockSkewMinutes { get; init; } = 5;
    public Dictionary<string, string> HmacKeys { get; init; } = new(StringComparer.Ordinal);

}