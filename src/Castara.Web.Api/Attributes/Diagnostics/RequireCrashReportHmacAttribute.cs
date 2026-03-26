namespace Castara.Web.Api.Attributes.Diagnostics;

/// <summary>
/// Marker attribute that indicates an endpoint requires HMAC-SHA256 signature validation for crash report submissions.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used to mark controller actions or entire controllers that handle crash report
/// submissions and require HMAC signature validation to ensure request authenticity and prevent
/// unauthorized access.
/// </para>
/// 
/// <para>
/// <b>How it works:</b>
/// </para>
/// When applied to a controller or action, this attribute is detected by the middleware pipeline's
/// conditional branching logic (see <c>WebApplicationExtensions.ConfigureCrashReportHMACMiddleware</c>).
/// The middleware inspects the matched endpoint's metadata and applies HMAC validation only when this
/// attribute is present.
/// 
/// <para>
/// <b>Attribute usage:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>AttributeTargets.Method</b>: Can be applied to individual controller actions</description></item>
/// <item><description><b>AttributeTargets.Class</b>: Can be applied to entire controllers (affects all actions)</description></item>
/// <item><description><b>AllowMultiple = false</b>: Only one instance allowed per target</description></item>
/// <item><description><b>Inherited = true</b>: Inherited by derived classes</description></item>
/// </list>
/// 
/// <para>
/// <b>Required HTTP headers for HMAC validation:</b>
/// </para>
/// Endpoints marked with this attribute must include the following headers:
/// <list type="bullet">
/// <item><description><b>X-Castara-Key-Id</b>: Identifies which shared secret to use for validation</description></item>
/// <item><description><b>X-Castara-Timestamp</b>: ISO 8601 timestamp for replay attack prevention</description></item>
/// <item><description><b>X-Castara-Signature</b>: Hex-encoded HMAC-SHA256 signature of "timestamp\nbody"</description></item>
/// </list>
/// 
/// <para>
/// <b>Security validation:</b>
/// </para>
/// When this attribute is present, the middleware validates:
/// <list type="number">
/// <item><description>Presence of all required headers</description></item>
/// <item><description>Key ID exists in configured HMAC keys</description></item>
/// <item><description>Timestamp is within allowed clock skew window (prevents replay attacks)</description></item>
/// <item><description>Signature matches computed HMAC-SHA256 of request payload</description></item>
/// </list>
/// 
/// <para>
/// <b>Example usage on a controller:</b>
/// </para>
/// <code>
/// [ApiController]
/// [Route("api/v{version:apiVersion}/diagnostics/crash-reports")]
/// [RequireCrashReportHmac]  // ← All actions in this controller require HMAC validation
/// public class CrashReportController : ControllerBase
/// {
///     [HttpPost]
///     public async Task&lt;IActionResult&gt; Submit(
///         [FromBody] SubmitCrashReportRequest request)
///     {
///         // HMAC validation already completed by middleware
///         // Request is authenticated if we reach here
///         return Ok();
///     }
/// }
/// </code>
/// 
/// <para>
/// <b>Example usage on a specific action:</b>
/// </para>
/// <code>
/// [ApiController]
/// [Route("api/v{version:apiVersion}/diagnostics")]
/// public class DiagnosticsController : ControllerBase
/// {
///     [HttpPost("crash-reports")]
///     [RequireCrashReportHmac]  // ← Only this action requires HMAC validation
///     public async Task&lt;IActionResult&gt; SubmitCrashReport(
///         [FromBody] SubmitCrashReportRequest request)
///     {
///         // HMAC validated
///         return Ok();
///     }
///     
///     [HttpGet("health")]  // ← This action does NOT require HMAC validation
///     public IActionResult GetHealth()
///     {
///         return Ok();
///     }
/// }
/// </code>
/// 
/// <para>
/// <b>Mixed usage (class and method level):</b>
/// </para>
/// <code>
/// [ApiController]
/// [Route("api/v{version:apiVersion}/diagnostics")]
/// [RequireCrashReportHmac]  // ← Default: all actions require HMAC
/// public class DiagnosticsController : ControllerBase
/// {
///     [HttpPost("crash-reports")]
///     public async Task&lt;IActionResult&gt; SubmitCrashReport(...)
///     {
///         // HMAC validated (inherited from class)
///         return Ok();
///     }
/// }
/// </code>
/// 
/// <para>
/// <b>Integration with middleware:</b>
/// </para>
/// The middleware checks for this attribute during request processing:
/// <code>
/// app.UseWhen(
///     context => context.GetEndpoint()?.Metadata
///         .GetMetadata&lt;RequireCrashReportHmacAttribute&gt;() is not null,
///     branch => branch.UseMiddleware&lt;CrashReportHmacValidationMiddleware&gt;()
/// );
/// </code>
/// 
/// <para>
/// <b>Benefits of this approach:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Selective validation</b>: HMAC validation only runs for marked endpoints</description></item>
/// <item><description><b>Performance</b>: Non-crash-report endpoints skip expensive cryptographic validation</description></item>
/// <item><description><b>Clarity</b>: Attribute clearly indicates security requirements</description></item>
/// <item><description><b>Flexibility</b>: Easy to add/remove HMAC requirement without code changes</description></item>
/// <item><description><b>Separation of concerns</b>: Security logic isolated from business logic</description></item>
/// </list>
/// 
/// <para>
/// <b>Security considerations:</b>
/// </para>
/// <list type="bullet">
/// <item><description>This attribute should be applied to all crash report submission endpoints</description></item>
/// <item><description>HMAC keys must be securely stored (Azure Key Vault, AWS Secrets Manager)</description></item>
/// <item><description>Clock skew should be minimal (typically 5 minutes) to prevent replay attacks</description></item>
/// <item><description>Signature validation uses constant-time comparison to prevent timing attacks</description></item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireCrashReportHmacAttribute : Attribute
{
}