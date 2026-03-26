using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using System.IO;
using System.Text.RegularExpressions;

namespace Castara.Api.Diagnostics.Services;

/// <summary>
/// Provides server-side sanitization of crash reports to ensure privacy-sensitive information
/// such as file paths and usernames are redacted before storage or processing.
/// </summary>
/// <remarks>
/// This service provides defense-in-depth sanitization. While clients should sanitize data
/// before submission, this server-side sanitizer ensures that no sensitive information slips
/// through, protecting user privacy even if client-side sanitization is bypassed or incomplete.
/// File paths are replaced with tokens while preserving filenames for debugging purposes.
/// </remarks>
public sealed class CrashReportSanitizer : ICrashReportSanitizer
{
    /// <summary>
    /// Regex pattern for matching Windows file paths (e.g., C:\Users\Name\file.txt).
    /// Matches paths followed by line numbers or at word boundaries.
    /// </summary>
    private static readonly Regex WindowsPathRegex = new(
        @"(?<!\w)([A-Za-z]:\\[^\r\n\t""<>|]*?)(?=:\d+|:\s*line\s+\d+|\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Regex pattern for matching UNC (Universal Naming Convention) paths (e.g., \\server\share\file.txt).
    /// </summary>
    private static readonly Regex UncPathRegex = new(
        @"(?<!\w)(\\\\[^\s\r\n\t""<>|]+(?:\\[^\s\r\n\t""<>|]+)*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Regex pattern for matching Unix/Linux file paths (e.g., /home/user/file.txt).
    /// Matches paths followed by line numbers or at word boundaries.
    /// </summary>
    private static readonly Regex UnixPathRegex = new(
        @"(?<!\w)(/(?:[^/\s:\r\n]+/)*[^/\s:\r\n]+)(?=:\d+|:\s*line\s+\d+|\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Sanitizes a crash report submission request by redacting file paths and usernames
    /// from all text fields while preserving filenames for debugging.
    /// </summary>
    /// <param name="request">The crash report submission request to sanitize.</param>
    /// <returns>A new <see cref="SubmitCrashReportRequest"/> with all sensitive information redacted.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> or <paramref name="request.Report"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Sanitizes the following fields:
    /// <list type="bullet">
    /// <item><description>Exception messages and stack traces (primary and inner exceptions)</description></item>
    /// <item><description>Application state context values</description></item>
    /// <item><description>Log entry categories and messages</description></item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// Sanitizes a crash exception info DTO by redacting sensitive information from the message and stack trace.
    /// </summary>
    /// <param name="value">The exception info to sanitize.</param>
    /// <returns>A new <see cref="CrashExceptionInfoDto"/> with sanitized message and stack trace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    private CrashExceptionInfoDto Sanitize(CrashExceptionInfoDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value with
        {
            Message = SanitizeText(value.Message) ?? string.Empty,
            StackTrace = SanitizeText(value.StackTrace)
        };
    }

    /// <summary>
    /// Sanitizes a crash log entry DTO by redacting sensitive information from the category and message.
    /// </summary>
    /// <param name="value">The log entry to sanitize.</param>
    /// <returns>A new <see cref="CrashLogEntryDto"/> with sanitized category and message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    private CrashLogEntryDto Sanitize(CrashLogEntryDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value with
        {
            Category = SanitizeText(value.Category) ?? string.Empty,
            Message = SanitizeText(value.Message) ?? string.Empty
        };
    }

    /// <summary>
    /// Sanitizes all values in an application state context dictionary.
    /// </summary>
    /// <param name="context">The context dictionary to sanitize.</param>
    /// <returns>A new dictionary with all values sanitized.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    private IReadOnlyDictionary<string, string> SanitizeContext(
        IReadOnlyDictionary<string, string> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ToDictionary(
            kvp => kvp.Key,
            kvp => SanitizeText(kvp.Value) ?? string.Empty,
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Sanitizes text by redacting usernames and file system paths while preserving filenames.
    /// </summary>
    /// <param name="value">The text to sanitize.</param>
    /// <returns>
    /// The sanitized text with usernames replaced with "[redacted-user]" and paths redacted,
    /// or the original value if it's <see langword="null"/> or whitespace.
    /// </returns>
    private static string? SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var sanitized = value;

        // Replace any occurrences of the current username (from server environment)
        if (!string.IsNullOrWhiteSpace(Environment.UserName))
        {
            sanitized = sanitized.Replace(
                Environment.UserName,
                "[redacted-user]",
                StringComparison.OrdinalIgnoreCase);
        }

        // Replace Windows paths (C:\path\to\file) with [redacted-path]\file
        sanitized = WindowsPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        // Replace UNC paths (\\server\share\file) with [redacted-unc-path]\file
        sanitized = UncPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-unc-path]"));

        // Replace Unix paths (/home/user/file) with [redacted-path]/file
        sanitized = UnixPathRegex.Replace(sanitized, match =>
            RedactPath(match.Value, "[redacted-path]"));

        return sanitized;
    }

    /// <summary>
    /// Redacts a file path by replacing the directory structure with a token while preserving the filename.
    /// </summary>
    /// <param name="path">The file path to redact.</param>
    /// <param name="token">The redaction token to use (e.g., "[redacted-path]").</param>
    /// <returns>
    /// A string with the format "token\filename" if a filename exists, or just the token if no filename is present.
    /// </returns>
    private static string RedactPath(string path, string token)
    {
        // Normalize path separators to platform-specific separator
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);

        var fileName = Path.GetFileName(normalized);

        // Preserve the filename for debugging while hiding the directory structure
        return string.IsNullOrWhiteSpace(fileName)
            ? token
            : $"{token}{Path.DirectorySeparatorChar}{fileName}";
    }
}