namespace Castara.Web.Api.Dtos.HealthReport;

public sealed class HealthReportDto
{
    public string Status { get; set; } = String.Empty;

    public double TotalDuration { get; set; }

    public IEnumerable<HealthReportEntryDto> Checks { get; set; } = Array.Empty<HealthReportEntryDto>();

    public string TraceId { get; set; } = String.Empty;
}

