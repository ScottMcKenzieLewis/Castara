using Microsoft.Extensions.Primitives;
using NUlid;

namespace Castara.Api.Middleware;

/// <summary>
/// Middleware for managing correlation IDs across distributed requests and services.
/// </summary>
/// <remarks>
/// This middleware ensures that every HTTP request has a unique correlation ID that can be
/// used to trace the request's journey through multiple services, microservices, and system
/// components. Correlation IDs are essential for distributed tracing and troubleshooting in
/// microservice architectures.
/// 
/// The middleware implements the following logic:
/// <list type="number">
/// <item><description>Checks if the incoming request has an X-Correlation-ID header</description></item>
/// <item><description>If present and valid, uses that correlation ID (for request continuity)</description></item>
/// <item><description>If absent or empty, generates a new ULID as the correlation ID</description></item>
/// <item><description>Stores the correlation ID in HttpContext.Items for access by other components</description></item>
/// <item><description>Adds the correlation ID to the response headers so clients can track requests</description></item>
/// <item><description>Creates a log scope with the correlation ID for automatic inclusion in all logs</description></item>
/// </list>
/// 
/// Benefits of correlation IDs:
/// <list type="bullet">
/// <item><description>Trace requests across multiple services and systems</description></item>
/// <item><description>Correlate logs from different services for a single user action</description></item>
/// <item><description>Debug issues in distributed systems by following the request path</description></item>
/// <item><description>Support customers by having them provide the correlation ID from error messages</description></item>
/// <item><description>Analyze request flows and performance bottlenecks</description></item>
/// </list>
/// 
/// Why ULID instead of GUID:
/// <list type="bullet">
/// <item><description>Lexicographically sortable (timestamp-based ordering)</description></item>
/// <item><description>More compact and URL-safe representation</description></item>
/// <item><description>Can be used for chronological sorting and time-based analysis</description></item>
/// <item><description>128-bit like UUIDs but with better human readability</description></item>
/// </list>
/// 
/// This middleware should be registered very early in the pipeline (in Program.cs) so that
/// the correlation ID is available to all subsequent middleware and logging operations.
/// </remarks>
/// <example>
/// Request flow with correlation ID:
/// <code>
/// // Client sends request without correlation ID
/// GET /api/v1/bonds/value
/// 
/// // Middleware generates ULID: 01HN3KQVMQXYZ5N8J7G2P4W6ST
/// // Response includes the correlation ID
/// HTTP/1.1 200 OK
/// X-Correlation-ID: 01HN3KQVMQXYZ5N8J7G2P4W6ST
/// 
/// // All logs for this request include:
/// // [INF] {CorrelationId=01HN3KQVMQXYZ5N8J7G2P4W6ST} Processing bond valuation...
/// </code>
/// 
/// Request continuity across services:
/// <code>
/// // Client forwards correlation ID to maintain traceability
/// GET /api/v1/bonds/value
/// X-Correlation-ID: 01HN3KQVMQXYZ5N8J7G2P4W6ST
/// 
/// // Middleware reuses existing correlation ID
/// // Enables tracing across service boundaries
/// </code>
/// </example>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance for recording correlation information.</param>
    /// <remarks>
    /// This constructor is called by the ASP.NET Core dependency injection container
    /// when the middleware is registered. The <paramref name="next"/> delegate represents
    /// the next piece of middleware in the pipeline.
    /// </remarks>
    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to establish or propagate a correlation ID for the current request.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item><description>Checks for an existing X-Correlation-ID header in the request</description></item>
    /// <item><description>Uses the existing ID if valid, or generates a new ULID if absent/empty</description></item>
    /// <item><description>Stores the correlation ID in HttpContext.Items[<c>"X-Correlation-ID"</c>]</description></item>
    /// <item><description>Adds the correlation ID to response headers for client tracking</description></item>
    /// <item><description>Creates a log scope containing both CorrelationId and TraceId</description></item>
    /// <item><description>Invokes the next middleware with the correlation context established</description></item>
    /// </list>
    /// 
    /// Correlation ID vs. Trace ID:
    /// <list type="bullet">
    /// <item><description><b>Correlation ID</b>: User-defined or generated, propagates across services, persists in logs and responses</description></item>
    /// <item><description><b>Trace ID</b>: ASP.NET Core generated per request, used for local request tracking</description></item>
    /// </list>
    /// 
    /// Both IDs are included in the log scope to provide multiple correlation options:
    /// <list type="bullet">
    /// <item><description>Use CorrelationId for cross-service distributed tracing</description></item>
    /// <item><description>Use TraceId for local application request correlation</description></item>
    /// </list>
    /// 
    /// The log scope ensures that all logs generated during request processing automatically
    /// include the CorrelationId and TraceId properties, making it easy to filter and correlate
    /// logs in centralized logging systems like Application Insights, Elasticsearch, or Splunk.
    /// </remarks>
    /// <example>
    /// Accessing the correlation ID in a controller or service:
    /// <code>
    /// public class MyController : ControllerBase
    /// {
    ///     public IActionResult GetData()
    ///     {
    ///         var correlationId = HttpContext.Items["X-Correlation-ID"] as string;
    ///         // Use correlation ID for logging, error messages, etc.
    ///         return Ok(new { correlationId });
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task Invoke(HttpContext context)
    {
        string correlationId;

        // Check if the request already contains a correlation ID
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            // Reuse existing correlation ID for request continuity across services
            correlationId = headerValue!;
        }
        else
        {
            // Generate a new ULID for this request
            correlationId = Ulid.NewUlid().ToString();
        }

        // Store correlation ID in HttpContext.Items for access by other components
        context.Items[HeaderName] = correlationId;

        // Add correlation ID to response headers so clients can track their requests
        context.Response.Headers[HeaderName] = correlationId;

        // Create a log scope with both correlation ID and trace ID for comprehensive tracing
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier
        }))
        {
            // Invoke the next middleware with correlation context established
            await _next(context);
        }
    }
}