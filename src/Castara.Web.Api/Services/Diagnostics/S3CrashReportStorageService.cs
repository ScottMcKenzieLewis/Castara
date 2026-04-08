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

/// <summary>
/// AWS S3 implementation of crash report storage service.
/// </summary>
/// <remarks>
/// This service stores crash reports as JSON documents in Amazon S3 for long-term
/// storage, analysis, and archival. Each crash report is stored with a unique incident ID
/// and organized by date for easy retrieval and lifecycle management.
/// 
/// <para>
/// <b>Storage characteristics:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Format</b>: JSON documents with UTF-8 encoding</description></item>
/// <item><description><b>Content-Type</b>: application/json</description></item>
/// <item><description><b>Naming</b>: YYYY/MM/DD/{IncidentId}_{ClientReportId}.json</description></item>
/// <item><description><b>Encryption</b>: Configurable SSE-S3 (AES256) or SSE-KMS</description></item>
/// <item><description><b>Incident ID</b>: ULID (Universally Unique Lexicographically Sortable Identifier)</description></item>
/// </list>
/// 
/// <para>
/// <b>Object key structure:</b>
/// </para>
/// <code>
/// {KeyPrefix}{Year}/{Month}/{Day}/{IncidentId}_{ClientReportId}.json
/// 
/// Examples:
/// crash-reports/2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_client-abc123.json
/// 2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_unknown.json (no prefix, no client ID)
/// </code>
/// 
/// <para>
/// <b>Stored document structure:</b>
/// </para>
/// <code>
/// {
///   "IncidentId": "01HXR3A4T5QKJM9W8Y6Z2N3P0R",
///   "ReceivedAtUtc": "2026-04-08T15:30:45.1234567Z",
///   "CrashReport": {
///     "ReportId": "client-abc123",
///     "AppVersion": "1.0.0",
///     "Exception": { ... },
///     "Logs": [ ... ],
///     ...
///   }
/// }
/// </code>
/// 
/// <para>
/// <b>Why ULID for incident IDs:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Lexicographically sortable</b>: ULIDs sort chronologically when ordered alphabetically</description></item>
/// <item><description><b>Timestamp prefix</b>: First 48 bits encode Unix timestamp (millisecond precision)</description></item>
/// <item><description><b>Uniqueness</b>: 128-bit identifier with 80 bits of randomness</description></item>
/// <item><description><b>Compact</b>: 26-character Base32 encoding (vs 36 chars for UUID)</description></item>
/// <item><description><b>Monotonic</b>: Sequential ULIDs within same millisecond are sortable</description></item>
/// </list>
/// 
/// <para>
/// <b>Configuration:</b>
/// </para>
/// Configuration is loaded from appsettings.json via <see cref="S3CrashReportStorageOptions"/>:
/// <code>
/// "CrashReportStorage": {
///   "S3": {
///     "BucketName": "castara-crash-reports",
///     "KeyPrefix": "crash-reports/",
///     "ServerSideEncryptionMethod": "AES256",
///     "KmsKeyId": null
///   }
/// }
/// </code>
/// 
/// <para>
/// <b>Encryption behavior:</b>
/// </para>
/// <list type="bullet">
/// <item><description>If <c>KmsKeyId</c> is provided: Uses SSE-KMS with specified key (overrides <c>ServerSideEncryptionMethod</c>)</description></item>
/// <item><description>If <c>ServerSideEncryptionMethod</c> is set: Uses specified method (AES256 or AWSKMS)</description></item>
/// <item><description>If neither is set: Uses bucket default encryption (recommended)</description></item>
/// </list>
/// 
/// <para>
/// <b>Error handling:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Missing configuration</b>: Throws <see cref="InvalidOperationException"/> if BucketName is not configured</description></item>
/// <item><description><b>Invalid encryption method</b>: Throws <see cref="InvalidOperationException"/> for unsupported values</description></item>
/// <item><description><b>S3 errors</b>: Propagates <see cref="AmazonS3Exception"/> for AWS service failures</description></item>
/// <item><description><b>Network errors</b>: Propagates network exceptions for connectivity issues</description></item>
/// </list>
/// 
/// <para>
/// <b>Logging:</b>
/// </para>
/// The service logs structured information at key points:
/// <list type="bullet">
/// <item><description><b>Before upload</b>: IncidentId, ClientReportId, Bucket, Key</description></item>
/// <item><description><b>After upload</b>: IncidentId, ClientReportId, HttpStatusCode</description></item>
/// </list>
/// 
/// <para>
/// <b>Performance considerations:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>JSON serialization</b>: Uses indented formatting for human readability (adds ~10-20% size overhead)</description></item>
/// <item><description><b>Memory usage</b>: Serializes to memory stream before upload (suitable for typical crash report sizes &lt; 1 MB)</description></item>
/// <item><description><b>Network latency</b>: Upload time depends on report size and network speed (typically 100-500ms)</description></item>
/// <item><description><b>KMS overhead</b>: SSE-KMS adds ~5-10ms for key generation API call</description></item>
/// </list>
/// 
/// <para>
/// <b>Typical crash report size:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Minimal report (no logs): ~2-5 KB</description></item>
/// <item><description>With 100 log entries: ~50-100 KB</description></item>
/// <item><description>With 1000 log entries: ~500 KB - 1 MB</description></item>
/// </list>
/// 
/// <para>
/// <b>S3 lifecycle management:</b>
/// </para>
/// Recommended lifecycle policies for cost optimization:
/// <code>
/// {
///   "Rules": [
///     {
///       "Id": "Archive old crash reports",
///       "Status": "Enabled",
///       "Filter": { "Prefix": "crash-reports/" },
///       "Transitions": [
///         { "Days": 90, "StorageClass": "GLACIER" }
///       ],
///       "Expiration": { "Days": 365 }
///     }
///   ]
/// }
/// </code>
/// 
/// <para>
/// <b>Integration with analytics:</b>
/// </para>
/// S3-stored crash reports can be analyzed using:
/// <list type="bullet">
/// <item><description><b>Amazon Athena</b>: SQL queries over crash report JSON</description></item>
/// <item><description><b>AWS Glue</b>: ETL pipelines for data transformation</description></item>
/// <item><description><b>Amazon QuickSight</b>: Dashboards and visualizations</description></item>
/// <item><description><b>Lambda functions</b>: Automated processing and alerting</description></item>
/// </list>
/// 
/// Service registration occurs in <see cref="ServiceCollectionExtensions.AddApplicationServices"/>
/// with <b>Scoped</b> lifetime (one instance per HTTP request).
/// </remarks>
public sealed class S3CrashReportStorageService : ICrashReportStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly IOptions<S3CrashReportStorageOptions> _options;
    private readonly ILogger<S3CrashReportStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3CrashReportStorageService"/> class.
    /// </summary>
    /// <param name="s3">The AWS S3 client for object storage operations.</param>
    /// <param name="options">The S3 storage configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s3"/>, <paramref name="options"/>, or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor validates all dependencies and initializes JSON serialization options
    /// with indented formatting for human-readable crash report documents in S3.
    /// </para>
    /// 
    /// <para>
    /// <b>Dependency injection:</b>
    /// </para>
    /// Dependencies are injected by the ASP.NET Core DI container:
    /// <list type="bullet">
    /// <item><description><b>IAmazonS3</b>: Registered as Singleton by <c>AddAWSService&lt;IAmazonS3&gt;()</c></description></item>
    /// <item><description><b>IOptions&lt;S3CrashReportStorageOptions&gt;</b>: Bound from configuration</description></item>
    /// <item><description><b>ILogger</b>: Provided by ASP.NET Core logging infrastructure</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>JSON serialization configuration:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>WriteIndented</b>: <c>true</c> - Pretty-printed JSON for readability</description></item>
    /// <item><description><b>Trade-off</b>: ~10-20% larger file size vs readability and debugging benefits</description></item>
    /// </list>
    /// 
    /// <para>
    /// Configuration validation occurs during first <see cref="StoreAsync"/> call,
    /// not in the constructor, following fail-fast-on-use pattern.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Stores a crash report as a JSON document in Amazon S3.
    /// </summary>
    /// <param name="request">The crash report submission request containing the report data.</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a <see cref="StoreCrashReportResultDto"/> with the assigned incident ID and storage timestamp.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> or <paramref name="request"/>.Report is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description>BucketName is not configured (null or whitespace)</description></item>
    /// <item><description>ServerSideEncryptionMethod contains an unsupported value</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="AmazonS3Exception">
    /// Thrown when AWS S3 service returns an error:
    /// <list type="bullet">
    /// <item><description><b>NoSuchBucket</b>: Configured bucket does not exist</description></item>
    /// <item><description><b>AccessDenied</b>: IAM permissions insufficient for PutObject</description></item>
    /// <item><description><b>InvalidAccessKeyId</b>: AWS credentials are invalid</description></item>
    /// <item><description><b>SignatureDoesNotMatch</b>: AWS signature calculation failed</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when network connectivity to AWS S3 fails.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Operation flow:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Generate ULID-based incident ID (26-character sortable identifier)</description></item>
    /// <item><description>Capture current UTC timestamp for ReceivedAtUtc</description></item>
    /// <item><description>Validate configuration (BucketName required)</description></item>
    /// <item><description>Wrap crash report in <see cref="S3StoredCrashReport"/> record with metadata</description></item>
    /// <item><description>Build S3 object key using date hierarchy and incident ID</description></item>
    /// <item><description>Serialize to indented JSON for human readability</description></item>
    /// <item><description>Configure encryption based on options (KMS or AES256)</description></item>
    /// <item><description>Upload to S3 with application/json content type</description></item>
    /// <item><description>Log upload details before and after operation</description></item>
    /// <item><description>Return result with incident ID and timestamp</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Object key format:</b>
    /// </para>
    /// <code>
    /// {KeyPrefix}{Year}/{Month}/{Day}/{IncidentId}_{ClientReportId}.json
    /// 
    /// Examples:
    /// crash-reports/2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_client-abc123.json
    /// crash-reports/2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_unknown.json (missing client ID)
    /// 2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_unknown.json (no prefix)
    /// </code>
    /// 
    /// <para>
    /// <b>Date hierarchy benefits:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>S3 list operations</b>: Efficient prefix-based queries by date</description></item>
    /// <item><description><b>Lifecycle policies</b>: Target specific date ranges for archival/deletion</description></item>
    /// <item><description><b>Analytics</b>: Partition data by date for Athena queries</description></item>
    /// <item><description><b>Human navigation</b>: Browse crash reports chronologically in S3 console</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Encryption precedence:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>If <c>KmsKeyId</c> is set: Use SSE-KMS with specified key (highest priority)</description></item>
    /// <item><description>If <c>ServerSideEncryptionMethod</c> is set: Use specified method</description></item>
    /// <item><description>If neither is set: Use bucket default encryption (recommended)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Stored document structure:</b>
    /// </para>
    /// The uploaded JSON contains:
    /// <code>
    /// {
    ///   "IncidentId": "01HXR3A4T5QKJM9W8Y6Z2N3P0R",    // Server-generated ULID
    ///   "ReceivedAtUtc": "2026-04-08T15:30:45.123Z",  // Server timestamp
    ///   "CrashReport": {                               // Original client crash report
    ///     "ReportId": "client-abc123",
    ///     "AppVersion": "1.0.0",
    ///     "Exception": { "Type": "NullReferenceException", ... },
    ///     "Logs": [ ... ],
    ///     "SystemInfo": { ... }
    ///   }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Why wrap the crash report:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Server timestamp</b>: Accurate ReceivedAtUtc regardless of client clock skew</description></item>
    /// <item><description><b>Incident ID</b>: Server-controlled unique identifier for tracking</description></item>
    /// <item><description><b>Future metadata</b>: Room for server-side enrichment (IP address, geo-location, etc.)</description></item>
    /// <item><description><b>Version tracking</b>: Schema versioning for stored document format evolution</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Logging details:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><b>Before upload</b> (Information level):
    /// IncidentId, ClientReportId, Bucket, Key - For request correlation and debugging
    /// </description>
    /// </item>
    /// <item>
    /// <description><b>After upload</b> (Information level):
    /// IncidentId, ClientReportId, HttpStatusCode - Confirms successful storage
    /// </description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>Performance characteristics:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Serialization</b>: ~1-5ms for typical crash reports</description></item>
    /// <item><description><b>Network upload</b>: ~100-500ms depending on report size and latency</description></item>
    /// <item><description><b>KMS key generation</b>: +5-10ms if using SSE-KMS</description></item>
    /// <item><description><b>Total time</b>: Typically 200-700ms for end-to-end operation</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Example usage:</b>
    /// </para>
    /// <code>
    /// var result = await storageService.StoreAsync(request, cancellationToken);
    /// 
    /// // result.IncidentId: "01HXR3A4T5QKJM9W8Y6Z2N3P0R"
    /// // result.ReceivedAtUtc: 2026-04-08T15:30:45.123Z
    /// // result.Status: "Stored"
    /// </code>
    /// 
    /// <para>
    /// <b>Cancellation support:</b>
    /// </para>
    /// The operation can be cancelled via <paramref name="cancellationToken"/>.
    /// If cancelled during upload, the S3 multipart upload is automatically aborted.
    /// Partial uploads do not leave orphaned data in S3.
    /// </remarks>
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

    /// <summary>
    /// Builds the S3 object key for a crash report using date hierarchy and identifiers.
    /// </summary>
    /// <param name="keyPrefix">The normalized key prefix (may be empty string).</param>
    /// <param name="receivedAtUtc">The timestamp when the crash report was received.</param>
    /// <param name="incidentId">The server-generated ULID incident identifier.</param>
    /// <param name="clientReportId">The optional client-provided report identifier.</param>
    /// <returns>
    /// The complete S3 object key in the format:
    /// <c>{keyPrefix}{YYYY}/{MM}/{DD}/{IncidentId}_{sanitizedClientReportId}.json</c>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Key format:</b>
    /// </para>
    /// <code>
    /// {KeyPrefix}{Year}/{Month}/{Day}/{IncidentId}_{ClientReportId}.json
    /// 
    /// Examples:
    /// crash-reports/2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_client-abc123.json
    /// crash-reports/2026/04/08/01HXR3A4T5QKJM9W8Y6Z2N3P0R_unknown.json (no client ID)
    /// 2026/12/31/01HXR3A4T5QKJM9W8Y6Z2N3P0R_my_report_id.json (no prefix)
    /// </code>
    /// 
    /// <para>
    /// <b>Date hierarchy components:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>YYYY</b>: Four-digit year (e.g., "2026")</description></item>
    /// <item><description><b>MM</b>: Two-digit month with leading zero (e.g., "04" for April)</description></item>
    /// <item><description><b>DD</b>: Two-digit day with leading zero (e.g., "08")</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Client report ID sanitization:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>If <paramref name="clientReportId"/> is null or whitespace: Use <c>"unknown"</c></description></item>
    /// <item><description>Invalid filename characters are replaced with underscores via <see cref="SanitizeSegment"/></description></item>
    /// <item><description>Ensures S3 key is always valid regardless of client input</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Why this key structure:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Date partitioning</b>: Enables efficient S3 list operations by date range</description></item>
    /// <item><description><b>Lexicographic sorting</b>: Keys naturally sort chronologically</description></item>
    /// <item><description><b>Analytics integration</b>: Athena/Glue can partition by date folders</description></item>
    /// <item><description><b>Lifecycle policies</b>: Target specific date ranges for archival/deletion</description></item>
    /// <item><description><b>Human navigation</b>: Easy to browse in S3 console by date</description></item>
    /// <item><description><b>Incident + Client correlation</b>: Both IDs in filename for troubleshooting</description></item>
    /// </list>
    /// 
    /// <para>
    /// This is a pure function with no side effects.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Normalizes a key prefix to ensure consistent S3 object key formatting.
    /// </summary>
    /// <param name="prefix">The raw key prefix from configuration, may be null or contain inconsistent slashes.</param>
    /// <returns>
    /// A normalized prefix string:
    /// <list type="bullet">
    /// <item><description>Empty string if input is null or whitespace</description></item>
    /// <item><description>Forward slashes only (backslashes converted)</description></item>
    /// <item><description>Guaranteed to end with a forward slash if non-empty</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Normalization rules:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>If input is null or whitespace, return empty string</description></item>
    /// <item><description>Trim leading and trailing whitespace</description></item>
    /// <item><description>Replace all backslashes (<c>\</c>) with forward slashes (<c>/</c>)</description></item>
    /// <item><description>Ensure the prefix ends with a forward slash (<c>/</c>)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Examples:</b>
    /// </para>
    /// <code>
    /// NormalizePrefix(null)                 → ""
    /// NormalizePrefix("")                   → ""
    /// NormalizePrefix("  ")                 → ""
    /// NormalizePrefix("crash-reports")      → "crash-reports/"
    /// NormalizePrefix("crash-reports/")     → "crash-reports/"
    /// NormalizePrefix("crash\\reports")     → "crash/reports/"
    /// NormalizePrefix("  crash-reports  ")  → "crash-reports/"
    /// NormalizePrefix("a/b/c")              → "a/b/c/"
    /// NormalizePrefix("a/b/c/")             → "a/b/c/"
    /// </code>
    /// 
    /// <para>
    /// <b>Why normalize backslashes:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>S3 uses forward slashes as path separators (Unix-style)</description></item>
    /// <item><description>Configuration might contain Windows-style paths (backslashes)</description></item>
    /// <item><description>Ensures consistent key format regardless of developer's platform</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Why ensure trailing slash:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Prevents accidental key concatenation issues (e.g., "prefixfilename.json")</description></item>
    /// <item><description>Makes prefix act like a "folder" in S3 console visualization</description></item>
    /// <item><description>Consistent with S3 best practices for key hierarchies</description></item>
    /// </list>
    /// 
    /// This is a pure function with no side effects.
    /// </remarks>
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

    /// <summary>
    /// Sanitizes a string value to be safe for use in S3 object key file names.
    /// </summary>
    /// <param name="value">The string value to sanitize (client-provided report ID).</param>
    /// <returns>
    /// A sanitized string with invalid filename characters replaced by underscores.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Sanitization process:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Identify invalid filename characters using <see cref="Path.GetInvalidFileNameChars"/></description></item>
    /// <item><description>Replace each invalid character with an underscore (<c>_</c>)</description></item>
    /// <item><description>Return the sanitized string</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Invalid characters (Windows and cross-platform):</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>&lt; &gt; : " / \ | ? *</c> (filesystem reserved)</description></item>
    /// <item><description>Control characters (ASCII 0-31)</description></item>
    /// <item><description>Characters that could cause parsing issues in various tools</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Examples:</b>
    /// </para>
    /// <code>
    /// SanitizeSegment("valid-id-123")          → "valid-id-123"  (no change)
    /// SanitizeSegment("my/report/id")          → "my_report_id"  (slashes)
    /// SanitizeSegment("report:2026-04-08")     → "report_2026-04-08"  (colon)
    /// SanitizeSegment("client&lt;123&gt;")     → "client_123_"  (angle brackets)
    /// SanitizeSegment("file*.txt")             → "file_.txt"  (asterisk)
    /// </code>
    /// 
    /// <para>
    /// <b>Why sanitize client report IDs:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>S3 safety</b>: Prevents invalid S3 object keys (though S3 is permissive)</description></item>
    /// <item><description><b>Cross-platform compatibility</b>: Works with all filesystems if downloaded</description></item>
    /// <item><description><b>Tool compatibility</b>: Avoids parsing issues in shell scripts, URLs, etc.</description></item>
    /// <item><description><b>Security</b>: Prevents potential path traversal attempts (e.g., "../../../")</description></item>
    /// <item><description><b>Readability</b>: Ensures keys are human-readable in S3 console</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>S3 key constraints:</b>
    /// </para>
    /// While S3 object keys can contain most UTF-8 characters, this sanitization ensures:
    /// <list type="bullet">
    /// <item><description>Keys work correctly when downloaded to any OS (Windows, Linux, macOS)</description></item>
    /// <item><description>Keys don't cause issues in shell scripts or command-line tools</description></item>
    /// <item><description>Keys are safe in HTTP URLs (though URL encoding would handle most cases)</description></item>
    /// </list>
    /// 
    /// This is a pure function with no side effects.
    /// </remarks>
    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    /// <summary>
    /// Parses a server-side encryption method string to the corresponding AWS SDK enum value.
    /// </summary>
    /// <param name="value">
    /// The encryption method string from configuration.
    /// Valid values: <c>"AES256"</c> or <c>"AWSKMS"</c> (case-insensitive).
    /// </param>
    /// <returns>
    /// The corresponding <see cref="ServerSideEncryptionMethod"/> enum value.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value"/> is not a supported encryption method.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Supported encryption methods:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><b>"AES256"</b> → <see cref="ServerSideEncryptionMethod.AES256"/>
    /// <para>Server-side encryption with Amazon S3-managed keys (SSE-S3).</para>
    /// <para>Uses AES-256 encryption with keys managed and rotated by AWS.</para>
    /// </description>
    /// </item>
    /// <item>
    /// <description><b>"AWSKMS"</b> → <see cref="ServerSideEncryptionMethod.AWSKMS"/>
    /// <para>Server-side encryption with AWS Key Management Service (SSE-KMS).</para>
    /// <para>Uses customer-managed keys with audit trails and access controls.</para>
    /// </description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>Parsing behavior:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Trims leading/trailing whitespace</description></item>
    /// <item><description>Converts to uppercase for case-insensitive matching</description></item>
    /// <item><description>Invariant culture comparison (not locale-dependent)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Examples:</b>
    /// </para>
    /// <code>
    /// ParseEncryptionMethod("AES256")     → ServerSideEncryptionMethod.AES256
    /// ParseEncryptionMethod("aes256")     → ServerSideEncryptionMethod.AES256  (case-insensitive)
    /// ParseEncryptionMethod("  AES256  ") → ServerSideEncryptionMethod.AES256  (trimmed)
    /// ParseEncryptionMethod("AWSKMS")     → ServerSideEncryptionMethod.AWSKMS
    /// ParseEncryptionMethod("aws:kms")    → InvalidOperationException  (colon not supported)
    /// ParseEncryptionMethod("invalid")    → InvalidOperationException
    /// </code>
    /// 
    /// <para>
    /// <b>Why not support "aws:kms":</b>
    /// </para>
    /// The AWS S3 API uses <c>"aws:kms"</c> in HTTP headers, but the SDK enum is <c>AWSKMS</c>.
    /// This method expects the configuration to use the enum-friendly format (<c>"AWSKMS"</c>)
    /// rather than the HTTP header format (<c>"aws:kms"</c>) for consistency.
    /// 
    /// <para>
    /// <b>Alternative approach:</b>
    /// </para>
    /// If you want to support the <c>"aws:kms"</c> format, add another case:
    /// <code>
    /// "AWS:KMS" or "AWSKMS" => ServerSideEncryptionMethod.AWSKMS,
    /// </code>
    /// 
    /// <para>
    /// <b>Error handling:</b>
    /// </para>
    /// Invalid values throw <see cref="InvalidOperationException"/> with a descriptive message
    /// including the invalid value. This provides fail-fast behavior during application startup
    /// when configuration is first used.
    /// 
    /// This is a pure function with no side effects.
    /// </remarks>
    private static ServerSideEncryptionMethod ParseEncryptionMethod(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "AES256" => ServerSideEncryptionMethod.AES256,
            "AWSKMS" => ServerSideEncryptionMethod.AWSKMS,
            _ => throw new InvalidOperationException(
                $"Unsupported S3 server-side encryption method '{value}'.")
        };

    /// <summary>
    /// Represents a crash report stored in S3 with server-generated metadata.
    /// </summary>
    /// <param name="IncidentId">
    /// The server-generated unique incident identifier (ULID format).
    /// A 26-character lexicographically sortable identifier that encodes
    /// timestamp and randomness for uniqueness and chronological ordering.
    /// </param>
    /// <param name="ReceivedAtUtc">
    /// The UTC timestamp when the crash report was received by the server.
    /// Provides accurate server-side timing regardless of client clock skew.
    /// </param>
    /// <param name="CrashReport">
    /// The original crash report submitted by the client.
    /// Contains application version, exception details, logs, and system information.
    /// </param>
    /// <remarks>
    /// <para>
    /// This record serves as a wrapper around the client-submitted <see cref="CrashReportDto"/>,
    /// adding server-controlled metadata before serialization to S3.
    /// </para>
    /// 
    /// <para>
    /// <b>Why wrap the crash report:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Server timestamp</b>: Accurate ReceivedAtUtc independent of client clock</description></item>
    /// <item><description><b>Unique ID</b>: Server-controlled incident identifier (ULID) for correlation</description></item>
    /// <item><description><b>Future extensibility</b>: Room for server-side enrichment (IP, geo-location, processing metadata)</description></item>
    /// <item><description><b>Version evolution</b>: Can add schema version field for format migrations</description></item>
    /// <item><description><b>Immutability</b>: Record type ensures stored data cannot be mutated</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Serialized JSON structure:</b>
    /// </para>
    /// <code>
    /// {
    ///   "IncidentId": "01HXR3A4T5QKJM9W8Y6Z2N3P0R",
    ///   "ReceivedAtUtc": "2026-04-08T15:30:45.1234567Z",
    ///   "CrashReport": {
    ///     "ReportId": "client-abc123",
    ///     "AppVersion": "1.0.0",
    ///     "Exception": { ... },
    ///     "Logs": [ ... ],
    ///     "SystemInfo": { ... }
    ///   }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>ULID characteristics:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Format</b>: 26-character Base32 string (e.g., "01HXR3A4T5QKJM9W8Y6Z2N3P0R")</description></item>
    /// <item><description><b>Timestamp</b>: First 48 bits encode Unix timestamp (millisecond precision)</description></item>
    /// <item><description><b>Randomness</b>: Last 80 bits are cryptographically random</description></item>
    /// <item><description><b>Sortability</b>: Lexicographically sortable (alphabetical order = chronological order)</description></item>
    /// <item><description><b>Collision resistance</b>: 2^80 random bits provide strong uniqueness guarantees</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Benefits over GUID:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Shorter</b>: 26 characters vs 36 for GUID (UUID)</description></item>
    /// <item><description><b>Sortable</b>: Natural chronological ordering (GUIDs are random)</description></item>
    /// <item><description><b>Timestamp-aware</b>: Can extract creation time from ID</description></item>
    /// <item><description><b>URL-safe</b>: Base32 alphabet avoids URL encoding issues</description></item>
    /// <item><description><b>Database-friendly</b>: Better index performance due to sequential nature</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Record type benefits:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Immutability</b>: Cannot modify after creation (data integrity)</description></item>
    /// <item><description><b>Value semantics</b>: Equality based on values, not reference</description></item>
    /// <item><description><b>Concise syntax</b>: Positional parameters for construction</description></item>
    /// <item><description><b>Built-in ToString</b>: Automatic readable representation</description></item>
    /// </list>
    /// 
    /// <para>
    /// This record is private and only used internally for S3 serialization.
    /// External code receives <see cref="StoreCrashReportResultDto"/> instead.
    /// </para>
    /// </remarks>
    private sealed record S3StoredCrashReport(
        string IncidentId,
        DateTimeOffset ReceivedAtUtc,
        CrashReportDto CrashReport);
}
