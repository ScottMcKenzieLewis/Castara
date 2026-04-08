using Amazon.S3;
using Amazon.S3.Model;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Microsoft.Extensions.Options;
using NUlid;
using System.Text;
using System.Text.Json;

namespace Castara.Web.Api.Services.Diagnostics;

public sealed class S3CrashReportStorageService : ICrashReportStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly IOptions<S3CrashReportStorageOptions> _options;
    private readonly ILogger<S3CrashReportStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public S3CrashReportStorageService(
        IAmazonS3 s3,
        IOptions<S3CrashReportStorageOptions> options,
        ILogger<S3CrashReportStorageService> logger)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task<StoreCrashReportResultDto> StoreAsync(
        SubmitCrashReportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Report);

        var nowUtc = DateTimeOffset.UtcNow;
        var incidentId = Ulid.NewUlid().ToString();

        var bucketName = _options.Value.BucketName;
        var keyPrefix = NormalizePrefix(_options.Value.KeyPrefix);

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException(
                "S3 crash report storage is not configured. BucketName is required.");
        }

        var storageRecord = new S3StoredCrashReport(
            incidentId,
            nowUtc,
            request.Report);

        var key = BuildObjectKey(
            keyPrefix,
            nowUtc,
            incidentId,
            request.Report.ReportId);

        var json = JsonSerializer.Serialize(storageRecord, _jsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        };

        if (!string.IsNullOrWhiteSpace(_options.Value.ServerSideEncryptionMethod))
        {
            putRequest.ServerSideEncryptionMethod =
                ParseEncryptionMethod(_options.Value.ServerSideEncryptionMethod);
        }

        if (!string.IsNullOrWhiteSpace(_options.Value.KmsKeyId))
        {
            putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
            putRequest.ServerSideEncryptionKeyManagementServiceKeyId = _options.Value.KmsKeyId;
        }

        _logger.LogInformation(
            "Storing crash report in S3. IncidentId={IncidentId}, ClientReportId={ClientReportId}, Bucket={Bucket}, Key={Key}",
            incidentId,
            request.Report.ReportId,
            bucketName,
            key);

        var response = await _s3.PutObjectAsync(putRequest, cancellationToken);

        _logger.LogInformation(
            "Crash report stored in S3. IncidentId={IncidentId}, ClientReportId={ClientReportId}, HttpStatusCode={HttpStatusCode}",
            incidentId,
            request.Report.ReportId,
            response.HttpStatusCode);

        return new StoreCrashReportResultDto(
            incidentId,
            nowUtc,
            "Stored");
    }

    private static string BuildObjectKey(
        string keyPrefix,
        DateTimeOffset receivedAtUtc,
        string incidentId,
        string? clientReportId)
    {
        var safeClientReportId = string.IsNullOrWhiteSpace(clientReportId)
            ? "unknown"
            : SanitizeSegment(clientReportId);

        return $"{keyPrefix}{receivedAtUtc:yyyy/MM/dd}/{incidentId}_{safeClientReportId}.json";
    }

    private static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        prefix = prefix.Trim().Replace("\\", "/", StringComparison.Ordinal);

        if (!prefix.EndsWith("/", StringComparison.Ordinal))
        {
            prefix += "/";
        }

        return prefix;
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static ServerSideEncryptionMethod ParseEncryptionMethod(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "AES256" => ServerSideEncryptionMethod.AES256,
            "AWSKMS" => ServerSideEncryptionMethod.AWSKMS,
            _ => throw new InvalidOperationException(
                $"Unsupported S3 server-side encryption method '{value}'.")
        };

    private sealed record S3StoredCrashReport(
        string IncidentId,
        DateTimeOffset ReceivedAtUtc,
        CrashReportDto CrashReport);
}
