using Castara.Api.Configuration;
using Castara.Api.Middleware.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Castara.Api.Services.Diagnostics;

/// <summary>
/// Validates HMAC-SHA256 signatures on crash report submission requests to ensure authenticity
/// and prevent unauthorized access and replay attacks.
/// </summary>
/// <remarks>
/// <para>
/// The validator expects three HTTP headers on incoming requests:
/// </para>
/// <list type="bullet">
/// <item><term>X-Castara-Key-Id:</term><description>Identifies which shared secret to use for validation</description></item>
/// <item><term>X-Castara-Timestamp:</term><description>ISO 8601 timestamp for replay attack prevention</description></item>
/// <item><term>X-Castara-Signature:</term><description>Hex-encoded HMAC-SHA256 signature of "timestamp\nbody"</description></item>
/// </list>
/// <para>
/// The signature is computed as: <c>HMAC-SHA256(secret, timestamp + "\n" + requestBody)</c> and
/// encoded as uppercase hexadecimal. Timestamp must be within the configured clock skew window
/// to prevent replay attacks.
/// </para>
/// </remarks>
public sealed class CrashReportRequestSignatureValidator
    : ICrashReportRequestSignatureValidator
{
    /// <summary>
    /// HTTP header name for the key identifier used to look up the shared secret.
    /// </summary>
    public const string KeyIdHeaderName = "X-Castara-Key-Id";

    /// <summary>
    /// HTTP header name for the request timestamp in ISO 8601 format (UTC).
    /// </summary>
    public const string TimestampHeaderName = "X-Castara-Timestamp";

    /// <summary>
    /// HTTP header name for the HMAC-SHA256 signature (hex-encoded, uppercase).
    /// </summary>
    public const string SignatureHeaderName = "X-Castara-Signature";

    private readonly CrashReportIngestionOptions _options;

    private readonly ILogger<CrashReportRequestSignatureValidator> _logger;


    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportRequestSignatureValidator"/> class.
    /// </summary>
    /// <param name="options">Configuration options containing HMAC keys and clock skew settings.</param>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    public CrashReportRequestSignatureValidator(
        IOptions<CrashReportIngestionOptions> options,
        ILogger<CrashReportRequestSignatureValidator> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<CrashReportRequestSignatureValidator>.Instance;
    }

    /// <summary>
    /// Validates the HMAC-SHA256 signature and timestamp of an HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// <see langword="true"/> if all required headers are present, the key ID is recognized,
    /// the timestamp is within the allowed clock skew window, and the signature matches;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// <strong>Validation Steps:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>Verifies presence of all three required headers (Key-Id, Timestamp, Signature)</description></item>
    /// <item><description>Looks up the shared secret using the provided Key-Id</description></item>
    /// <item><description>Parses and validates the timestamp is within allowed clock skew window (prevents replay attacks)</description></item>
    /// <item><description>Reads and buffers the request body (enables body reuse by downstream middleware)</description></item>
    /// <item><description>Computes expected HMAC-SHA256 signature of "timestamp\nbody" payload</description></item>
    /// <item><description>Performs constant-time comparison of expected vs. provided signature (prevents timing attacks)</description></item>
    /// </list>
    /// <para>
    /// <strong>Security Note:</strong> This method enables request body buffering, allowing the body to be read
    /// multiple times. The body position is reset after reading to ensure downstream handlers can still access it.
    /// </para>
    /// </remarks>
    public async Task<bool> IsValidAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Extract required headers
        var keyId = request.Headers[KeyIdHeaderName].FirstOrDefault();
        _logger.LogInformation("Key ID: {KeyId}", keyId);

        var timestampText = request.Headers[TimestampHeaderName].FirstOrDefault();
        _logger.LogInformation("Timestamp: {Timestamp}", timestampText);

        var providedSignature = request.Headers[SignatureHeaderName].FirstOrDefault();
        _logger.LogInformation("Signature: {Signature}", providedSignature);

        // Validate header presence
        if (string.IsNullOrWhiteSpace(keyId) ||
            string.IsNullOrWhiteSpace(timestampText) ||
            string.IsNullOrWhiteSpace(providedSignature))
        {
            return false;
        }

        // Lookup shared key by key ID
        if (!_options.HmacKeys.TryGetValue(keyId, out var hmacKey) ||
            string.IsNullOrWhiteSpace(hmacKey))
        {
            return false;
        }

        _logger.LogInformation("HMAC Key: {HmacKey}", hmacKey);

        // Parse timestamp
        if (!DateTimeOffset.TryParse(
                timestampText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            return false;
        }

        // Validate timestamp is within allowed clock skew window (prevents replay attacks)
        var now = DateTimeOffset.UtcNow;
        var skew = TimeSpan.FromMinutes(_options.AllowedClockSkewMinutes);

        if (timestamp < now - skew || timestamp > now + skew)
        {
            return false;
        }

        // Enable buffering to allow request body to be read multiple times
        request.EnableBuffering();

        // Read request body
        string body;
        using (var reader = new StreamReader(
                   request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        // Reset body position for downstream middleware/controllers
        request.Body.Position = 0;

        // Compute expected HMAC signature
        var payload = $"{timestampText}\n{body}";
        var expectedSignature = ComputeSignature(hmacKey, payload);

        // Perform constant-time comparison to prevent timing attacks
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(providedSignature);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <summary>
    /// Computes an HMAC-SHA256 signature for the given payload using the specified secret.
    /// </summary>
    /// <param name="secret">The shared secret key used for HMAC computation.</param>
    /// <param name="payload">The payload string to sign (typically "timestamp\nbody").</param>
    /// <returns>The hex-encoded (uppercase) HMAC-SHA256 signature.</returns>
    /// <remarks>
    /// The signature is computed by:
    /// <list type="number">
    /// <item><description>Converting the secret and payload to UTF-8 byte arrays</description></item>
    /// <item><description>Computing HMAC-SHA256 hash of the payload using the secret</description></item>
    /// <item><description>Converting the hash to an uppercase hexadecimal string</description></item>
    /// </list>
    /// </remarks>
    private static string ComputeSignature(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        // Convert hash to uppercase hex string
        return Convert.ToHexString(hash);
    }
}