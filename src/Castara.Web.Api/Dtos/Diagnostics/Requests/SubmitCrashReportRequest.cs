using Castara.Web.Api.Dtos.Diagnostics;

namespace Castara.Web.Api.Dtos.Diagnostics.Requests;

public sealed record SubmitCrashReportRequest(
    CrashReportDto Report);
