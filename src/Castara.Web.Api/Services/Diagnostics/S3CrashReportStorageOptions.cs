namespace Castara.Web.Api.Services.Diagnostics;

/// <summary>
/// Configuration options for AWS S3 crash report storage.
/// </summary>
/// <remarks>
/// This class represents the configuration settings required for storing crash reports
/// in Amazon S3. Configuration values are bound from the "CrashReportStorage:S3" section
/// of appsettings.json using the options pattern.
/// 
/// <para>
/// <b>Configuration structure (appsettings.json):</b>
/// </para>
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
/// <b>Encryption options:</b>
/// </para>
/// Two encryption methods are supported:
/// <list type="bullet">
/// <item>
/// <description><b>AES256 (SSE-S3)</b>: Server-side encryption with Amazon S3-managed keys.
/// Set <see cref="ServerSideEncryptionMethod"/> to "AES256". No KMS key required.
/// Best for general use with built-in encryption at rest.</description>
/// </item>
/// <item>
/// <description><b>aws:kms (SSE-KMS)</b>: Server-side encryption with AWS KMS-managed keys.
/// Set <see cref="ServerSideEncryptionMethod"/> to "aws:kms" and provide <see cref="KmsKeyId"/>.
/// Best for compliance requirements needing audit trails and key rotation policies.</description>
/// </item>
/// </list>
/// 
/// <para>
/// <b>Example configurations:</b>
/// </para>
/// <code>
/// // Simple AES256 encryption (recommended for most scenarios)
/// "S3": {
///   "BucketName": "castara-crash-reports",
///   "KeyPrefix": "crash-reports/",
///   "ServerSideEncryptionMethod": "AES256"
/// }
/// 
/// // KMS encryption with customer-managed key
/// "S3": {
///   "BucketName": "castara-crash-reports",
///   "KeyPrefix": "crash-reports/",
///   "ServerSideEncryptionMethod": "aws:kms",
///   "KmsKeyId": "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012"
/// }
/// 
/// // No explicit encryption (uses bucket default encryption)
/// "S3": {
///   "BucketName": "castara-crash-reports",
///   "KeyPrefix": "crash-reports/"
/// }
/// </code>
/// 
/// <para>
/// <b>Environment-specific configuration:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Development</b>: Local bucket or LocalStack, no encryption required</description></item>
/// <item><description><b>Staging</b>: Separate staging bucket, AES256 encryption</description></item>
/// <item><description><b>Production</b>: Production bucket with KMS encryption for compliance</description></item>
/// </list>
/// 
/// <para>
/// <b>S3 object key structure:</b>
/// </para>
/// Objects are stored with the following key format:
/// <code>
/// {KeyPrefix}{AppName}/{Version}/{Timestamp:yyyyMMdd}/{Guid}.json
/// 
/// Example:
/// crash-reports/Castara/1.0.0/20260408/a1b2c3d4-e5f6-7890-abcd-ef1234567890.json
/// </code>
/// 
/// <para>
/// <b>Required AWS permissions:</b>
/// </para>
/// The IAM role or user must have:
/// <list type="bullet">
/// <item><description><b>s3:PutObject</b> on {BucketName}/{KeyPrefix}*</description></item>
/// <item><description><b>kms:GenerateDataKey</b> on KMS key (if using KMS encryption)</description></item>
/// </list>
/// 
/// <para>
/// <b>Configuration validation:</b>
/// </para>
/// The <see cref="S3CrashReportStorageService"/> validates this configuration at runtime:
/// <list type="bullet">
/// <item><description><see cref="BucketName"/> must not be null or empty</description></item>
/// <item><description><see cref="BucketName"/> must be a valid S3 bucket name (3-63 chars, lowercase)</description></item>
/// <item><description>If <see cref="ServerSideEncryptionMethod"/> is "aws:kms", <see cref="KmsKeyId"/> must be provided</description></item>
/// </list>
/// 
/// Configuration is registered in <see cref="WebApplicationBuilderExtensions.AddCrashReportStorageOptions"/>
/// and injected via <c>IOptions&lt;S3CrashReportStorageOptions&gt;</c>.
/// </remarks>
public sealed class S3CrashReportStorageOptions
{
    /// <summary>
    /// The configuration section name for S3 crash report storage options.
    /// </summary>
    /// <remarks>
    /// This constant defines the path in appsettings.json where S3 configuration is located.
    /// The configuration binding uses this value to locate the correct section:
    /// <code>
    /// builder.Services.Configure&lt;S3CrashReportStorageOptions&gt;(
    ///     builder.Configuration.GetSection(S3CrashReportStorageOptions.SectionName));
    /// </code>
    /// 
    /// Configuration path: <c>CrashReportStorage:S3</c>
    /// </remarks>
    public const string SectionName = "CrashReportStorage:S3";

    /// <summary>
    /// Gets or sets the name of the S3 bucket where crash reports will be stored.
    /// </summary>
    /// <value>
    /// The S3 bucket name. Must be a valid S3 bucket name following AWS naming rules:
    /// <list type="bullet">
    /// <item><description>Between 3 and 63 characters long</description></item>
    /// <item><description>Lowercase letters, numbers, hyphens, and periods only</description></item>
    /// <item><description>Must begin and end with a letter or number</description></item>
    /// <item><description>Cannot contain underscores or uppercase letters</description></item>
    /// <item><description>Cannot be formatted as an IP address</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// This is a <b>required</b> configuration value. If not provided or empty,
    /// the <see cref="S3CrashReportStorageService"/> will throw an exception during initialization.
    /// </para>
    /// 
    /// <para>
    /// <b>Bucket naming best practices:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Use descriptive names: <c>castara-crash-reports</c></description></item>
    /// <item><description>Include environment: <c>castara-crash-reports-prod</c></description></item>
    /// <item><description>Include region if multi-region: <c>castara-crash-reports-us-east-1</c></description></item>
    /// <item><description>Keep names globally unique (bucket names are global across all AWS accounts)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Security considerations:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Enable default encryption on the bucket (SSE-S3 or SSE-KMS)</description></item>
    /// <item><description>Block all public access to prevent accidental data exposure</description></item>
    /// <item><description>Enable versioning to protect against accidental deletions</description></item>
    /// <item><description>Configure lifecycle policies for automatic archival and deletion</description></item>
    /// </list>
    /// 
    /// Example valid bucket names:
    /// <list type="bullet">
    /// <item><description><c>castara-crash-reports</c></description></item>
    /// <item><description><c>my-company-app-crashes-prod</c></description></item>
    /// <item><description><c>crash-data.example.com</c></description></item>
    /// </list>
    /// 
    /// Example invalid bucket names:
    /// <list type="bullet">
    /// <item><description><c>Castara-Reports</c> (uppercase)</description></item>
    /// <item><description><c>crash_reports</c> (underscore)</description></item>
    /// <item><description><c>my</c> (too short)</description></item>
    /// </list>
    /// </remarks>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional key prefix for organizing crash reports within the S3 bucket.
    /// </summary>
    /// <value>
    /// The key prefix string, or <c>null</c> to store objects at the bucket root.
    /// Should typically end with a forward slash (<c>/</c>) for proper path organization.
    /// Default: <c>null</c>
    /// </value>
    /// <remarks>
    /// <para>
    /// The key prefix acts like a folder path within the S3 bucket, allowing multiple applications
    /// or environments to share the same bucket while maintaining logical separation.
    /// </para>
    /// 
    /// <para>
    /// <b>Common prefix patterns:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>crash-reports/</c> - Simple prefix for all crash reports</description></item>
    /// <item><description><c>crashes/production/</c> - Environment-specific prefix</description></item>
    /// <item><description><c>app/castara/crashes/</c> - Application and feature hierarchy</description></item>
    /// <item><description><c>null</c> or empty - Store at bucket root (not recommended for shared buckets)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Full object key format:</b>
    /// </para>
    /// <code>
    /// {KeyPrefix}{AppName}/{Version}/{Timestamp:yyyyMMdd}/{Guid}.json
    /// 
    /// Examples:
    /// - With prefix: crash-reports/Castara/1.0.0/20260408/abc123.json
    /// - Without prefix: Castara/1.0.0/20260408/abc123.json
    /// </code>
    /// 
    /// <para>
    /// <b>Lifecycle policy considerations:</b>
    /// </para>
    /// Using a consistent prefix allows targeted lifecycle policies:
    /// <code>
    /// // Archive all crash reports after 90 days
    /// {
    ///   "Rules": [{
    ///     "Id": "Archive crashes",
    ///     "Filter": { "Prefix": "crash-reports/" },
    ///     "Transitions": [{ "Days": 90, "StorageClass": "GLACIER" }]
    ///   }]
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>IAM policy targeting:</b>
    /// </para>
    /// Prefix-based permissions allow fine-grained access control:
    /// <code>
    /// {
    ///   "Effect": "Allow",
    ///   "Action": "s3:PutObject",
    ///   "Resource": "arn:aws:s3:::my-bucket/crash-reports/*"
    /// }
    /// </code>
    /// </remarks>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the server-side encryption method for crash report objects in S3.
    /// </summary>
    /// <value>
    /// The encryption method identifier, or <c>null</c> to use the bucket's default encryption.
    /// Valid values:
    /// <list type="bullet">
    /// <item><description><c>"AES256"</c> - Server-side encryption with Amazon S3-managed keys (SSE-S3)</description></item>
    /// <item><description><c>"aws:kms"</c> - Server-side encryption with AWS KMS-managed keys (SSE-KMS)</description></item>
    /// <item><description><c>null</c> - Use bucket default encryption (recommended)</description></item>
    /// </list>
    /// Default: <c>null</c>
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>Encryption comparison:</b>
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Method</term>
    /// <description>Use Case</description>
    /// </listheader>
    /// <item>
    /// <term><c>AES256</c> (SSE-S3)</term>
    /// <description>
    /// <b>General purpose:</b> Built-in encryption with Amazon-managed keys.
    /// No additional cost, no key management overhead. Suitable for most applications.
    /// Encryption keys are rotated automatically by AWS.
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>aws:kms</c> (SSE-KMS)</term>
    /// <description>
    /// <b>Compliance/audit:</b> Customer-managed keys with detailed audit trails.
    /// Requires <see cref="KmsKeyId"/>. Additional KMS API charges apply.
    /// Provides key rotation policies, access logging, and cross-account access control.
    /// Required for many compliance frameworks (HIPAA, PCI-DSS).
    /// </description>
    /// </item>
    /// <item>
    /// <term><c>null</c> (bucket default)</term>
    /// <description>
    /// <b>Recommended:</b> Uses whatever encryption is configured on the bucket.
    /// Simplifies configuration and allows centralized encryption policy management.
    /// If bucket has default encryption enabled, all objects inherit that setting.
    /// </description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>Configuration examples:</b>
    /// </para>
    /// <code>
    /// // Use bucket default (recommended)
    /// "S3": {
    ///   "BucketName": "castara-crash-reports",
    ///   "ServerSideEncryptionMethod": null
    /// }
    /// 
    /// // Explicit AES256 encryption
    /// "S3": {
    ///   "BucketName": "castara-crash-reports",
    ///   "ServerSideEncryptionMethod": "AES256"
    /// }
    /// 
    /// // KMS encryption with specific key
    /// "S3": {
    ///   "BucketName": "castara-crash-reports",
    ///   "ServerSideEncryptionMethod": "aws:kms",
    ///   "KmsKeyId": "arn:aws:kms:us-east-1:123456789012:key/12345678-..."
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Required IAM permissions:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>AES256:</b> Only <c>s3:PutObject</c> required</description></item>
    /// <item><description><b>aws:kms:</b> Requires <c>s3:PutObject</c> + <c>kms:GenerateDataKey</c> on the KMS key</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Validation rules:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>If <c>"aws:kms"</c>, <see cref="KmsKeyId"/> must be provided</description></item>
    /// <item><description>Invalid method values will cause runtime exceptions</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Performance considerations:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>SSE-S3 (AES256) has no performance impact</description></item>
    /// <item><description>SSE-KMS adds ~5-10ms latency per request for KMS API calls</description></item>
    /// <item><description>KMS has request rate limits (5,500-30,000 requests/sec depending on region)</description></item>
    /// </list>
    /// </remarks>
    public string? ServerSideEncryptionMethod { get; set; }

    /// <summary>
    /// Gets or sets the AWS KMS key identifier for server-side encryption with customer-managed keys.
    /// </summary>
    /// <value>
    /// The KMS key ARN, key ID, or alias, or <c>null</c> if not using KMS encryption.
    /// Required when <see cref="ServerSideEncryptionMethod"/> is <c>"aws:kms"</c>.
    /// Default: <c>null</c>
    /// </value>
    /// <remarks>
    /// <para>
    /// This property specifies which AWS KMS key to use for encrypting crash report objects
    /// when <see cref="ServerSideEncryptionMethod"/> is set to <c>"aws:kms"</c>.
    /// </para>
    /// 
    /// <para>
    /// <b>Valid key identifier formats:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Key ARN (recommended)</term>
    /// <description>Full Amazon Resource Name for the key. Most explicit and portable across regions.
    /// Format: <c>arn:aws:kms:{region}:{account-id}:key/{key-id}</c>
    /// Example: <c>arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Key ID</term>
    /// <description>Unique identifier for the key. Works only within the same AWS account and region.
    /// Format: <c>{key-id}</c>
    /// Example: <c>12345678-1234-1234-1234-123456789012</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Key Alias ARN</term>
    /// <description>ARN for a key alias. Useful for referencing keys by name.
    /// Format: <c>arn:aws:kms:{region}:{account-id}:alias/{alias-name}</c>
    /// Example: <c>arn:aws:kms:us-east-1:123456789012:alias/crash-reports-key</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Key Alias (not recommended)</term>
    /// <description>Alias name without ARN. Less explicit and may cause confusion.
    /// Format: <c>alias/{alias-name}</c>
    /// Example: <c>alias/crash-reports-key</c>
    /// </description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>Configuration examples:</b>
    /// </para>
    /// <code>
    /// // Using key ARN (recommended)
    /// "S3": {
    ///   "ServerSideEncryptionMethod": "aws:kms",
    ///   "KmsKeyId": "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012"
    /// }
    /// 
    /// // Using key ID
    /// "S3": {
    ///   "ServerSideEncryptionMethod": "aws:kms",
    ///   "KmsKeyId": "12345678-1234-1234-1234-123456789012"
    /// }
    /// 
    /// // Using alias ARN
    /// "S3": {
    ///   "ServerSideEncryptionMethod": "aws:kms",
    ///   "KmsKeyId": "arn:aws:kms:us-east-1:123456789012:alias/crash-reports-key"
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Required IAM permissions:</b>
    /// </para>
    /// The application's IAM role must have these permissions on the KMS key:
    /// <list type="bullet">
    /// <item><description><c>kms:GenerateDataKey</c> - Required for encrypting new objects</description></item>
    /// <item><description><c>kms:Decrypt</c> - Required only if retrieving objects (not needed for API)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Example KMS key policy:</b>
    /// </para>
    /// <code>
    /// {
    ///   "Version": "2012-10-17",
    ///   "Statement": [
    ///     {
    ///       "Sid": "Allow crash report API to encrypt",
    ///       "Effect": "Allow",
    ///       "Principal": {
    ///         "AWS": "arn:aws:iam::123456789012:role/CrashReportApiRole"
    ///       },
    ///       "Action": [
    ///         "kms:GenerateDataKey",
    ///         "kms:Decrypt"
    ///       ],
    ///       "Resource": "*"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Key management best practices:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Automatic rotation</b>: Enable automatic key rotation in KMS (rotates yearly)</description></item>
    /// <item><description><b>Key aliases</b>: Use aliases for easier key management and rotation</description></item>
    /// <item><description><b>Key policies</b>: Grant least-privilege access to only necessary IAM roles</description></item>
    /// <item><description><b>CloudTrail logging</b>: Monitor all KMS key usage for audit compliance</description></item>
    /// <item><description><b>Multi-region keys</b>: Use multi-region keys for disaster recovery scenarios</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Cost considerations:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>KMS key storage: $1/month per key</description></item>
    /// <item><description>API requests: $0.03 per 10,000 requests (GenerateDataKey for each upload)</description></item>
    /// <item><description>For high-volume scenarios, consider if KMS overhead is justified</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Validation:</b>
    /// </para>
    /// If <see cref="ServerSideEncryptionMethod"/> is <c>"aws:kms"</c> and this value is <c>null</c> or empty,
    /// the <see cref="S3CrashReportStorageService"/> will throw a configuration exception during initialization.
    /// </remarks>
    public string? KmsKeyId { get; set; }
}