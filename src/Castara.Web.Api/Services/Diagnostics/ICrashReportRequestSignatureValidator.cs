namespace Castara.Api.Services.Diagnostics;

public interface ICrashReportRequestSignatureValidator
{
    Task<bool> IsValidAsync(HttpRequest request, CancellationToken cancellationToken);
}
