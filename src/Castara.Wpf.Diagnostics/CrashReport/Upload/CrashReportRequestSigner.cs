using System.Security.Cryptography;
using System.Text;

namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public static class CrashReportRequestSigner
{
    public static (string Timestamp, string Signature) Sign(string secret, string jsonBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentNullException.ThrowIfNull(jsonBody);

        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var payload = $"{timestamp}\n{jsonBody}";

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return (timestamp, Convert.ToHexString(hash));
    }
}