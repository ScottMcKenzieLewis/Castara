using System.Security.Cryptography;
using System.Text;
using Castara.Api.Configuration;
using Castara.Api.Services.Diagnostics;
using Castara.Web.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Castara.Api.Tests.Services.Diagnostics;

public sealed class CrashReportRequestSignatureValidatorTests
{
    private readonly Mock<ILogger<CrashReportRequestSignatureValidator>> _loggerMock = new();
    private readonly Mock<IClock> _clockMock = new();

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        Action act = () => new CrashReportRequestSignatureValidator(
            options: null!,
            _loggerMock.Object,
            _clockMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenClockIsNull()
    {
        Action act = () => new CrashReportRequestSignatureValidator(
            Options.Create(CreateOptions()),
            _loggerMock.Object,
            clock: null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clock");
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenLoggerIsNull()
    {
        Action act = () => new CrashReportRequestSignatureValidator(
            Options.Create(CreateOptions()),
            logger: null!,
            _clockMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task IsValidAsync_ShouldThrow_WhenRequestIsNull()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.IsValidAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenKeyIdHeaderIsMissing()
    {
        var sut = CreateSut();
        var request = CreateRequest("{\"a\":1}");

        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = FixedNow.ToString("O");
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "ABC123";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampHeaderIsMissing()
    {
        var sut = CreateSut();
        var request = CreateRequest("{\"a\":1}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "ABC123";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenSignatureHeaderIsMissing()
    {
        var sut = CreateSut();
        var request = CreateRequest("{\"a\":1}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = FixedNow.ToString("O");

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenKeyIdIsUnknown()
    {
        var sut = CreateSut();
        var body = "{\"a\":1}";
        var timestamp = FixedNow.ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "unknown-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenConfiguredKeyIsBlank()
    {
        var options = new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>
            {
                ["test-key"] = "   "
            }
        };

        var sut = CreateSut(options);
        var request = CreateRequest("{\"a\":1}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = FixedNow.ToString("O");
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "IGNORED";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampCannotBeParsed()
    {
        var sut = CreateSut();
        var request = CreateRequest("{\"a\":1}");

        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = "not-a-timestamp";
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "ABC123";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampIsTooOld()
    {
        var sut = CreateSut();
        var body = "{\"a\":1}";
        var timestamp = FixedNow.AddMinutes(-6).ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenTimestampIsTooFarInFuture()
    {
        var sut = CreateSut();
        var body = "{\"a\":1}";
        var timestamp = FixedNow.AddMinutes(6).ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnTrue_WhenTimestampIsExactlyAtLowerBound()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.AddMinutes(-5).ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnTrue_WhenTimestampIsExactlyAtUpperBound()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.AddMinutes(5).ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnTrue_WhenHeadersTimestampAndSignatureAreValid()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidAsync_ShouldReturnFalse_WhenSignatureDoesNotMatch()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.ToString("O");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = "BAD_SIGNATURE";

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldTreatSignatureComparisonAsCaseSensitive()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.ToString("O");
        var validUppercaseSignature = ComputeSignature("secret-123", $"{timestamp}\n{body}");
        var lowercaseSignature = validUppercaseSignature.ToLowerInvariant();

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = lowercaseSignature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_ShouldResetBodyPosition_AfterReading()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestamp = FixedNow.ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestamp}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestamp;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
        request.Body.Position.Should().Be(0);

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var rereadBody = await reader.ReadToEndAsync();

        rereadBody.Should().Be(body);
    }

    [Fact]
    public async Task IsValidAsync_ShouldUseExactTimestampHeaderText_WhenComputingSignature()
    {
        var sut = CreateSut();
        var body = "{\"reportId\":\"abc123\"}";
        var timestampText = FixedNow.ToString("O");
        var signature = ComputeSignature("secret-123", $"{timestampText}\n{body}");

        var request = CreateRequest(body);
        request.Headers[CrashReportRequestSignatureValidator.KeyIdHeaderName] = "test-key";
        request.Headers[CrashReportRequestSignatureValidator.TimestampHeaderName] = timestampText;
        request.Headers[CrashReportRequestSignatureValidator.SignatureHeaderName] = signature;

        var result = await sut.IsValidAsync(request, CancellationToken.None);

        result.Should().BeTrue();
    }

    private CrashReportRequestSignatureValidator CreateSut(
        CrashReportIngestionOptions? options = null,
        DateTimeOffset? now = null)
    {
        options ??= CreateOptions();

        _clockMock
            .Setup(x => x.UtcNow)
            .Returns(now ?? FixedNow);

        return new CrashReportRequestSignatureValidator(
            Options.Create(options),
            _loggerMock.Object,
            _clockMock.Object);
    }

    private static readonly DateTimeOffset FixedNow =
        new(2026, 4, 8, 12, 0, 0, TimeSpan.Zero);

    private static CrashReportIngestionOptions CreateOptions()
    {
        return new CrashReportIngestionOptions
        {
            AllowedClockSkewMinutes = 5,
            HmacKeys = new Dictionary<string, string>
            {
                ["test-key"] = "secret-123"
            }
        };
    }

    private static HttpRequest CreateRequest(string body)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);

        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";

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