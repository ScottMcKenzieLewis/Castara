using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Services.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Castara.Web.Api.Tests.Services.Diagnostics;

public sealed class S3CrashReportStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3Mock = new();
    private readonly Mock<ILogger<S3CrashReportStorageService>> _loggerMock = new();

    [Fact]
    public void Constructor_ShouldThrow_WhenS3IsNull()
    {
        Action act = () => new S3CrashReportStorageService(
            s3: null!,
            Options.Create(CreateOptions()),
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("s3");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        Action act = () => new S3CrashReportStorageService(
            _s3Mock.Object,
            options: null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        Action act = () => new S3CrashReportStorageService(
            _s3Mock.Object,
            Options.Create(CreateOptions()),
            logger: null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task StoreAsync_ShouldThrow_WhenRequestIsNull()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.StoreAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task StoreAsync_ShouldUseUnknown_WhenClientReportIdIsWhitespace()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut();

        await sut.StoreAsync(CreateRequest("   "), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().EndWith("_unknown.json");
    }

    [Fact]
    public async Task StoreAsync_ShouldThrow_WhenBucketNameIsMissing()
    {
        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "   ",
            KeyPrefix = "crash-reports/"
        });

        var request = CreateRequest("client-123");

        Func<Task> act = async () => await sut.StoreAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BucketName is required*");
    }

    [Fact]
    public async Task StoreAsync_ShouldUploadJsonDocument_WithExpectedBucketContentTypeAndCancellationToken()
    {
        PutObjectRequest? capturedRequest = null;
        var cancellationToken = new CancellationTokenSource().Token;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports/"
        });

        var request = CreateRequest("client-123");

        var result = await sut.StoreAsync(request, cancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be("Stored");
        result.IncidentId.Should().NotBeNullOrWhiteSpace();
        result.IncidentId.Should().HaveLength(26);
        result.ReceivedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("castara-crash-reports");
        capturedRequest.ContentType.Should().Be("application/json");
        capturedRequest.Key.Should().StartWith("crash-reports/");
        capturedRequest.Key.Should().EndWith("_client-123.json");

        _s3Mock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task StoreAsync_ShouldNormalizePrefix_WhenPrefixHasNoTrailingSlash()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports"
        });

        await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().StartWith("crash-reports/");
    }

    [Fact]
    public async Task StoreAsync_ShouldNormalizePrefix_WhenPrefixUsesBackslashes()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = @"crash\reports"
        });

        await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().StartWith("crash/reports/");
        capturedRequest.Key.Should().NotContain("\\");
    }

    [Fact]
    public async Task StoreAsync_ShouldUseUnknown_WhenClientReportIdIsNull()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut();

        await sut.StoreAsync(CreateRequest(null), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().EndWith("_unknown.json");
    }

    [Fact]
    public async Task StoreAsync_ShouldSanitizeInvalidCharactersInClientReportId()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut();

        await sut.StoreAsync(CreateRequest(@"client:abc/123"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().Contain("_client");
        capturedRequest.Key.Should().EndWith(".json");
        capturedRequest.Key.Should().NotContain(":");
        capturedRequest.Key.Should().NotContain("\\");
    }

    [Fact]
    public async Task StoreAsync_ShouldApplyAes256Encryption_WhenConfigured()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports/",
            ServerSideEncryptionMethod = "AES256"
        });

        await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ServerSideEncryptionMethod.Should().Be(ServerSideEncryptionMethod.AES256);
        capturedRequest.ServerSideEncryptionKeyManagementServiceKeyId.Should().BeNull();
    }

    [Fact]
    public async Task StoreAsync_ShouldThrow_WhenEncryptionMethodIsUnsupported()
    {
        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports/",
            ServerSideEncryptionMethod = "banana"
        });

        Func<Task> act = async () => await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported S3 server-side encryption method*");
    }

    [Fact]
    public async Task StoreAsync_ShouldPreferKms_WhenKmsKeyIdIsProvided()
    {
        PutObjectRequest? capturedRequest = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut(new S3CrashReportStorageOptions
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports/",
            ServerSideEncryptionMethod = "AES256",
            KmsKeyId = "arn:aws:kms:us-east-1:123456789012:key/abc123"
        });

        await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ServerSideEncryptionMethod.Should().Be(ServerSideEncryptionMethod.AWSKMS);
        capturedRequest.ServerSideEncryptionKeyManagementServiceKeyId.Should().Be("arn:aws:kms:us-east-1:123456789012:key/abc123");
    }

    [Fact]
    public async Task StoreAsync_ShouldSerializeWrappedDocument_WithIncidentIdReceivedAtUtcAndCrashReport()
    {
        string? capturedJson = null;

        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) =>
            {
                req.InputStream.Should().NotBeNull();

                req.InputStream.Position = 0;

                using var reader = new StreamReader(
                    req.InputStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                capturedJson = reader.ReadToEnd();
                req.InputStream.Position = 0;
            })
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        var sut = CreateSut();
        var request = CreateRequest("client-123");

        var result = await sut.StoreAsync(request, CancellationToken.None);

        capturedJson.Should().NotBeNullOrWhiteSpace();
        capturedJson.Should().Contain("\"IncidentId\"");
        capturedJson.Should().Contain("\"ReceivedAtUtc\"");
        capturedJson.Should().Contain("\"CrashReport\"");
        capturedJson.Should().Contain("\"ReportId\"");
        capturedJson.Should().Contain("client-123");
        capturedJson.Should().Contain(result.IncidentId);
    }

    [Fact]
    public async Task StoreAsync_ShouldPropagateS3Exceptions()
    {
        _s3Mock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access denied"));

        var sut = CreateSut();

        Func<Task> act = async () => await sut.StoreAsync(CreateRequest("client-123"), CancellationToken.None);

        await act.Should().ThrowAsync<AmazonS3Exception>()
            .WithMessage("Access denied");
    }

    private S3CrashReportStorageService CreateSut(S3CrashReportStorageOptions? options = null)
    {
        options ??= CreateOptions();

        return new S3CrashReportStorageService(
            _s3Mock.Object,
            Options.Create(options),
            _loggerMock.Object);
    }

    private static S3CrashReportStorageOptions CreateOptions() =>
        new()
        {
            BucketName = "castara-crash-reports",
            KeyPrefix = "crash-reports/"
        };

    private static SubmitCrashReportRequest CreateRequest(string? reportId)
    {
        return new SubmitCrashReportRequest
        (
            Report: CreateCrashReport(reportId)
        );
    }

    private static CrashReportDto CreateCrashReport(string? reportId)
    {
        return new CrashReportDto(
            ReportId: reportId ?? string.Empty,
            TimestampUtc: new DateTimeOffset(2026, 4, 8, 12, 0, 0, TimeSpan.Zero),
            ApplicationName: "Castara.Wpf",
            ApplicationVersion: "1.0.0",
            RuntimeVersion: ".NET 8.0.0",
            OperatingSystem: "Windows 11",
            Source: "UnhandledException",
            Exception: new CrashExceptionInfoDto(
                Type: "System.InvalidOperationException",
                Message: "Something went wrong.",
                StackTrace: "at Castara.Somewhere.Method()"),
            InnerExceptions: Array.Empty<CrashExceptionInfoDto>(),
            Context: new Dictionary<string, string>
            {
                ["Theme"] = "Dark",
                ["View"] = "Calculations"
            },
            RecentLogs: new[]
            {
            new CrashLogEntryDto(
                TimestampUtc: new DateTimeOffset(2026, 4, 8, 11, 59, 0, TimeSpan.Zero),
                Level: "Information",
                Category: "Castara.Web.Api",
                Message: "Test log entry")
            });
    }
}