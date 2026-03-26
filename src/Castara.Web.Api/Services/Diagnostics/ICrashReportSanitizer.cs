namespace Castara.Api.Diagnostics.Services;

using Castara.Web.Api.Dtos.Diagnostics.Requests;

public interface ICrashReportSanitizer
{
    SubmitCrashReportRequest Sanitize(SubmitCrashReportRequest request);
}
