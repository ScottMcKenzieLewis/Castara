namespace Castara.Web.Api.Dtos.Diagnostics.Responses;

public sealed record SubmitCrashReportResponse(
    string IncidentId,
    DateTimeOffset ReceivedAtUtc,
    string Status);
