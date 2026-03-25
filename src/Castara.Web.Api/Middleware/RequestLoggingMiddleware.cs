using System.Diagnostics;

namespace Treasara.Api.Middleware;

/// <summary>
/// Middleware for logging incoming HTTP requests and their completion with timing information.
/// </summary>
/// <remarks>
/// This middleware intercepts all HTTP requests passing through the pipeline and logs:
/// <list type="bullet">
/// <item><description>Incoming request method and path</description></item>
/// <item><description>Request completion status code</description></item>
/// <item><description>Request processing duration in milliseconds</description></item>
/// <item><description>Correlation information via trace IDs</description></item>
/// </list>
/// 
/// The middleware uses structured logging with log scopes to attach contextual information
/// (TraceId, Method, Path) to all log entries generated during request processing. This enables:
/// <list type="bullet">
/// <item><description>Easy correlation of logs for a single request</description></item>
/// <item><description>Filtering and searching logs by request attributes</description></item>
/// <item><description>Distributed tracing across microservices</description></item>
/// <item><description>Performance monitoring and SLA tracking</description></item>
/// </list>
/// 
/// Request logging is essential for:
/// <list type="bullet">
/// <item><description>Debugging production issues by tracing request flows</description></item>
/// <item><description>Monitoring API performance and identifying slow endpoints</description></item>
/// <item><description>Security auditing and compliance</description></item>
/// <item><description>Analyzing usage patterns and traffic trends</description></item>
/// </list>
/// 
/// This middleware should be registered early in the pipeline (in Program.cs) to capture
/// all requests, including those that may fail in later middleware or endpoint handlers.
/// </remarks>
/// <example>
/// Register the middleware in Program.cs:
/// <code>
/// app.UseMiddleware&lt;RequestLoggingMiddleware&gt;();
/// </code>
/// 
/// Example log output:
/// <code>
/// [INF] Incoming request GET /api/v1/bonds/value
/// [INF] Completed request GET /api/v1/bonds/value with 200 in 145 ms
/// </code>
/// </example>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance for recording request information.</param>
    /// <remarks>
    /// This constructor is called by the ASP.NET Core dependency injection container
    /// when the middleware is registered. The <paramref name="next"/> delegate represents
    /// the next piece of middleware in the pipeline, which this middleware will invoke
    /// after performing its logging operations.
    /// </remarks>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to log request information and timing.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called by the ASP.NET Core pipeline for each incoming HTTP request.
    /// It performs the following steps:
    /// <list type="number">
    /// <item><description>Starts a stopwatch to measure request processing time</description></item>
    /// <item><description>Creates a log scope with TraceId, Method, and Path for correlation</description></item>
    /// <item><description>Logs the incoming request details</description></item>
    /// <item><description>Invokes the next middleware in the pipeline</description></item>
    /// <item><description>Logs the completed request with status code and elapsed time</description></item>
    /// </list>
    /// 
    /// The log scope ensures that all logs generated during request processing
    /// (by this middleware, other middleware, controllers, or services) automatically
    /// include the TraceId, Method, and Path properties. This makes it easy to filter
    /// and correlate logs for a specific request in log aggregation systems like
    /// Application Insights, Elasticsearch, or Splunk.
    /// 
    /// Performance considerations:
    /// <list type="bullet">
    /// <item><description>Uses Stopwatch for accurate millisecond timing</description></item>
    /// <item><description>Structured logging avoids string concatenation overhead</description></item>
    /// <item><description>Log scope is disposed automatically via using statement</description></item>
    /// <item><description>Minimal impact on request processing performance (&lt; 1ms overhead)</description></item>
    /// </list>
    /// </remarks>
    public async Task Invoke(HttpContext context)
    {
        // Start timing the request
        var stopwatch = Stopwatch.StartNew();

        // Create a log scope with contextual information for all logs during this request
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = context.TraceIdentifier,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.ToString()
        }))
        {
            // Log the incoming request
            _logger.LogInformation(
                "Incoming request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // Invoke the next middleware in the pipeline
            await _next(context);

            // Stop timing after the request completes
            stopwatch.Stop();

            // Log the completed request with status code and duration
            _logger.LogInformation(
                "Completed request {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}