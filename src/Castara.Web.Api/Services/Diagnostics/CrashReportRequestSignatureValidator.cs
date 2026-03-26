using Castara.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Castara.Api.Services.Diagnostics;

public sealed class CrashReportRequestSignatureValidator
    : ICrashReportRequestSignatureValidator
{
    public const string KeyIdHeaderName = "X-Castara-Key-Id";
    public const string TimestampHeaderName = "X-Castara-Timestamp";
    public const string SignatureHeaderName = "X-Castara-Signature";

    private readonly CrashReportIngestionOptions _options;

    public CrashReportRequestSignatureValidator(
        IOptions<CrashReportIngestionOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<bool> IsValidAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var keyId = request.Headers[KeyIdHeaderName].FirstOrDefault();
        var timestampText = request.Headers[TimestampHeaderName].FirstOrDefault();
        var providedSignature = request.Headers[SignatureHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(keyId) ||
            string.IsNullOrWhiteSpace(timestampText) ||
            string.IsNullOrWhiteSpace(providedSignature))
        {
            return false;
        }

        if (!_options.HmacKeys.TryGetValue(keyId, out var secret) ||
            string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                timestampText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var skew = TimeSpan.FromMinutes(_options.AllowedClockSkewMinutes);

        if (timestamp < now - skew || timestamp > now + skew)
        {
            return false;
        }

        request.EnableBuffering();

        string body;
        using (var reader = new StreamReader(
                   request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        request.Body.Position = 0;

        var payload = $"{timestampText}\n{body}";
        var expectedSignature = ComputeSignature(secret, payload);

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(providedSignature);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string ComputeSignature(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hash);
    }
}