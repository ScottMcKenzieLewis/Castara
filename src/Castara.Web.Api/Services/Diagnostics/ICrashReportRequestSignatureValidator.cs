namespace Castara.Api.Services.Diagnostics;

/// <summary>
/// Defines a contract for validating HMAC-SHA256 signatures on crash report submission requests
/// to ensure authenticity and prevent unauthorized access.
/// </summary>
/// <remarks>
/// Implementations should validate requests by:
/// <list type="number">
/// <item><description>Checking for required headers: X-Castara-Key-Id, X-Castara-Timestamp, X-Castara-Signature</description></item>
/// <item><description>Looking up the shared secret using the provided key ID</description></item>
/// <item><description>Validating the timestamp is within an acceptable clock skew window (prevents replay attacks)</description></item>
/// <item><description>Computing the expected HMAC-SHA256 signature of "timestamp\nbody" and comparing with the provided signature</description></item>
/// </list>
/// The signature format is: <c>HMAC-SHA256(secret, timestamp + "\n" + requestBody)</c> encoded as uppercase hexadecimal.
/// </remarks>
public interface ICrashReportRequestSignatureValidator
{
    /// <summary>
    /// Asynchronously validates the HMAC-SHA256 signature and timestamp of an HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <see langword="true"/> 
    /// if the request has valid headers, a recognized key ID, a timestamp within the allowed window, 
    /// and a matching signature; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Common validation failure scenarios include:
    /// <list type="bullet">
    /// <item><description>Missing or empty required headers</description></item>
    /// <item><description>Unrecognized key ID (not found in configured HMAC keys)</description></item>
    /// <item><description>Timestamp outside the allowed clock skew window (potential replay attack)</description></item>
    /// <item><description>Signature mismatch (tampered request or wrong secret)</description></item>
    /// </list>
    /// </remarks>
    Task<bool> IsValidAsync(HttpRequest request, CancellationToken cancellationToken);
}
