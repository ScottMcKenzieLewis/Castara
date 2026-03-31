/// <summary>
/// Represents the result of a crash report upload operation to the diagnostic server.
/// </summary>
/// <param name="Success">
/// Indicates whether the upload succeeded. 
/// True if the report was successfully transmitted and accepted by the server; otherwise, false.
/// </param>
/// <param name="IncidentId">
/// The unique incident identifier assigned by the server upon successful upload.
/// Null if the upload failed or the server did not provide an incident ID.
/// This ID can be used to correlate the client-side crash with server-side diagnostics.
/// </param>
/// <param name="Status">
/// The status message returned by the server, providing additional context about the result.
/// Typically contains "OK" on success or a brief status description.
/// Null if no status was provided or the upload failed before receiving a response.
/// </param>
/// <param name="ErrorMessage">
/// A descriptive error message if the upload failed.
/// Contains details about network errors, authentication failures, validation errors, or server rejections.
/// Null if the upload succeeded.
/// </param>
/// <remarks>
/// This record is returned by <see cref="CrashReportUploader"/> after attempting to upload
/// a crash report to the diagnostic API endpoint. It provides both success/failure indication
/// and detailed information for logging, user notification, or retry logic.
/// 
/// <para>
/// <b>Success Scenarios:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Success = true, IncidentId and Status populated, ErrorMessage = null</description></item>
/// </list>
/// 
/// <para>
/// <b>Failure Scenarios:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Network errors (timeout, connection refused, DNS failure)</description></item>
/// <item><description>Authentication failures (invalid HMAC signature)</description></item>
/// <item><description>Validation errors (invalid crash report format)</description></item>
/// <item><description>Server errors (HTTP 500, service unavailable)</description></item>
/// <item><description>Rate limiting (HTTP 429)</description></item>
/// </list>
/// </remarks>
public sealed record CrashReportUploadResult(
    bool Success,
    string? IncidentId,
    string? Status,
    string? ErrorMessage);