using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using System.IO;
using System.Text.RegularExpressions;

namespace Castara.Api.Diagnostics.Services;

public sealed class CrashReportSanitizer : ICrashReportSanitizer
{
    private static readonly Regex WindowsPathRegex = new(
        @"(?<!\w)([A-Za-z]:\\[^\r\n\t""<>|]*?)(?=:\d+|:\s*line\s+\d+|\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UncPathRegex = new(
        @"(?<!\w)(\\\\[^\s\r\n\t""<>|]+(?:\\[^\s\r\n\t""<>|]+)*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UnixPathRegex = new(
        @"(?<!\w)(/(?:[^/\s:\r\n]+/)*[^/\s:\r\n]+)(?=:\d+|:\s*line\s+\d+|\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SubmitCrashReportRequest Sanitize(SubmitCrashReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Report);

        var report = request.Report;

        var sanitizedReport = report with
        {
            Exception = Sanitize(report.Exception),
            InnerExceptions = report.InnerExceptions.Select(Sanitize).ToArray(),
            Context = SanitizeContext(report.Context),
            RecentLogs = report.RecentLogs.Select(Sanitize).ToArray()
        };

        return request with
        {
            Report = sanitizedReport
        };
    }

    private CrashExceptionInfoDto Sanitize(CrashExceptionInfoDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value with
        {
            Message = SanitizeText(value.Message) ?? string.Empty,
            StackTrace = SanitizeText(value.StackTrace)
        };
    }

    private CrashLogEntryDto Sanitize(CrashLogEntryDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value with
        {
            Category = SanitizeText(value.Category) ?? string.Empty,
            Message = SanitizeText(value.Message) ?? string.Empty
        };
    }

    private IReadOnlyDictionary<string, string> SanitizeContext(
        IReadOnlyDictionary<string, string> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ToDictionary(
            kvp => kvp.Key,
            kvp => SanitizeText(kvp.Value) ?? string.Empty,
            StringComparer.Ordinal);
    }

    private static string? SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = value;

        if (!string.IsNullOrWhiteSpace(Environment.UserName))
        {
            sanitized = sanitized.Replace(
                Environment.UserName,
                "[redacted-user]",
                StringComparison.OrdinalIgnoreCase);
        }

        sanitized = WindowsPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        sanitized = UncPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-unc-path]"));

        sanitized = UnixPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        return sanitized;
    }

    private static string RedactPath(string path, string token)
    {
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);

        var fileName = Path.GetFileName(normalized);

        return string.IsNullOrWhiteSpace(fileName)
            ? token
            : $"{token}{Path.DirectorySeparatorChar}{fileName}";
    }
}