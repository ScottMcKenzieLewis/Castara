using System.Security.Cryptography;
using System.Text;
using Castara.Api.Configuration;
using Castara.Api.Services.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Castara.Api.Tests.Services.Diagnostics;

public sealed class CrashReportRequestSignatureValidatorTests
{
    [Fact]
    public async Task IsValidAsync_ShouldReturnTrue_WhenHeadersAndSignatureAreValid()
    {
        var secret = "super-secret-key";
        var keyId = "castara-wpf-v1";
        var body = """{"report":{"reportId":"abc123"}}""";
        var timestamp = DateTimeOffset.UtcNow.ToString("O");

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [keyId] = secret
            }
        });

        var request = CreateRequest(body);
        var signature = ComputeSignature(secret, $"{timestamp}\n{body}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = keyId;
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
        request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenRequiredHeadersAreMissing()
    {
        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["castara-wpf-v1"] = "super-secret-key"
            }
        });

        var request = CreateRequest("""{"report":{"reportId":"abc123"}}""");

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenKeyIdIsUnknown()
    {
        var body = """{"report":{"reportId":"abc123"}}""";
        var timestamp = DateTimeOffset.UtcNow.ToString("O");

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["some-other-key"] = "super-secret-key"
            }
        });

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "castara-wpf-v1";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "ABC123";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampIsInvalid()
    {
        var body = """{"report":{"reportId":"abc123"}}""";

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["castara-wpf-v1"] = "super-secret-key"
            }
        });

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "castara-wpf-v1";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = "not-a-timestamp";
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "ABC123";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampIsTooOld()
    {
        var secret = "super-secret-key";
        var keyId = "castara-wpf-v1";
        var body = """{"report":{"reportId":"abc123"}}""";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [keyId] = secret
            }
        });

        var request = CreateRequest(body);
        var signature = ComputeSignature(secret, $"{timestamp}\n{body}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = keyId;
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenSignatureDoesNotMatch()
    {
        var secret = "super-secret-key";
        var keyId = "castara-wpf-v1";
        var body = """{"report":{"reportId":"abc123"}}""";
        var timestamp = DateTimeOffset.UtcNow.ToString("O");

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [keyId] = secret
            }
        });

        var request = CreateRequest(body);

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = keyId;
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "DEADBEEF";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenBodyIsModifiedAfterSigning()
    {
        var secret = "super-secret-key";
        var keyId = "castara-wpf-v1";
        var originalBody = """{"report":{"reportId":"abc123"}}""";
        var actualBody = """{"report":{"reportId":"xyz999"}}""";
        var timestamp = DateTimeOffset.UtcNow.ToString("O");

        var sut = CreateSut(new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [keyId] = secret
            }
        });

        var request = CreateRequest(actualBody);
        var signature = ComputeSignature(secret, $"{timestamp}\n{originalBody}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = keyId;
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    private static CrashReportRequestSignatureValidator CreateSut(CrashReportIngestionOptions options)
    {
        return new CrashReportRequestSignatureValidator(Options.Create(options));
    }

    private static HttpRequest CreateRequest(string body)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);

        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
        context.Request.Method = HttpMethods.Post;

        return context.Request;
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