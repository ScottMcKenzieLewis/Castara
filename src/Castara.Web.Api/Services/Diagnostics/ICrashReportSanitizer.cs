namespace Castara.Api.Diagnostics.Services;

using Castara.Web.Api.Dtos.Diagnostics.Requests;

/// <summary>
/// Defines a contract for sanitizing crash report submission requests to protect user privacy
/// by redacting file paths and usernames while preserving debugging information.
/// </summary>
/// <remarks>
/// <para>
/// This service provides defense-in-depth sanitization on the server side. While client applications
/// should sanitize crash reports before submission, this interface ensures that the server has a
/// final opportunity to redact sensitive information before storage or processing.
/// </para>
/// <para>
/// Sanitization includes:
/// </para>
/// <list type="bullet">
/// <item><description>Redacting file paths (e.g., C:\Users\Name\path → [redacted-path]\filename)</description></item>
/// <item><description>Redacting usernames (e.g., ScottLewis → [redacted-user])</description></item>
/// <item><description>Preserving filenames for debugging purposes</description></item>
/// </list>
/// <para>
/// This approach protects against malicious or compromised clients that might attempt to submit
/// unsanitized or partially sanitized crash reports containing sensitive user information.
/// </para>
/// </remarks>
public interface ICrashReportSanitizer
{
    /// <summary>
    /// Sanitizes a crash report submission request by redacting file paths and usernames
    /// from all text fields while preserving filenames for debugging purposes.
    /// </summary>
    /// <param name="request">The crash report submission request to sanitize.</param>
    /// <returns>
    /// A new <see cref="SubmitCrashReportRequest"/> instance with all sensitive information redacted.
    /// The original request is not modified.
    /// </returns>
    /// <remarks>
    /// The following fields are sanitized:
    /// <list type="bullet">
    /// <item><description>Exception messages and stack traces (primary exception)</description></item>
    /// <item><description>Inner exception messages and stack traces</description></item>
    /// <item><description>Application state context values</description></item>
    /// <item><description>Log entry categories and messages</description></item>
    /// </list>
    /// </remarks>
    SubmitCrashReportRequest Sanitize(SubmitCrashReportRequest request);
}
