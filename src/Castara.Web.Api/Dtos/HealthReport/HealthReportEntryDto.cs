namespace Castara.Web.Api.Dtos.HealthReport;

public sealed class HealthReportEntryDto
{
    public string Name { get; set; } = String.Empty;

    public string? Description { get; set; } = String.Empty;

    public string Status { get; set; } = String.Empty;

    public string TraceId { get; set; } = String.Empty;

    public double Duration { get; set; }
}
