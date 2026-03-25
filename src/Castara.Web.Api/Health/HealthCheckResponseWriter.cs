using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Castara.Api.Health;

/// <summary>
/// Provides custom JSON formatting for ASP.NET Core health check responses.
/// </summary>
/// <remarks>
/// This static class customizes the default health check endpoint response format
/// to provide a more detailed and consistent JSON structure. The custom format includes:
/// <list type="bullet">
/// <item><description>Overall health status (Healthy, Degraded, or Unhealthy)</description></item>
/// <item><description>Total duration of all health checks</description></item>
/// <item><description>Individual check results with names, statuses, descriptions, and durations</description></item>
/// <item><description>Trace ID for diagnostic correlation</description></item>
/// </list>
/// 
/// This enhanced format is useful for:
/// <list type="bullet">
/// <item><description>Monitoring systems that need structured health data</description></item>
/// <item><description>Debugging health check failures with detailed timing information</description></item>
/// <item><description>Correlating health check results with application logs via trace IDs</description></item>
/// <item><description>Creating dashboards that display individual component health</description></item>
/// </list>
/// 
/// Health checks are typically used to verify:
/// <list type="bullet">
/// <item><description>Database connectivity and responsiveness</description></item>
/// <item><description>External service availability (APIs, message queues, caches)</description></item>
/// <item><description>File system access and disk space</description></item>
/// <item><description>Memory and CPU utilization thresholds</description></item>
/// </list>
/// </remarks>
public static class HealthCheckResponseWriter
{
    /// <summary>
    /// Writes a custom JSON health check response to the HTTP response stream.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="report">The health report containing aggregated health check results.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <remarks>
    /// This method is typically registered with the health check middleware in Program.cs:
    /// <code>
    /// app.MapHealthChecks("/health", new HealthCheckOptions
    /// {
    ///     ResponseWriter = HealthCheckResponseWriter.WriteResponse
    /// });
    /// </code>
    /// 
    /// The generated JSON structure:
    /// <code>
    /// {
    ///   "status": "Healthy",
    ///   "totalDuration": 125.5,
    ///   "checks": [
    ///     {
    ///       "name": "database",
    ///       "status": "Healthy",
    ///       "description": "Database connection successful",
    ///       "duration": 45.2
    ///     },
    ///     {
    ///       "name": "external-api",
    ///       "status": "Healthy",
    ///       "description": "External API responding",
    ///       "duration": 80.3
    ///     }
    ///   ],
    ///   "traceId": "0HMVFE3A4TQKJ:00000001"
    /// }
    /// </code>
    /// 
    /// Status values:
    /// <list type="bullet">
    /// <item><description><b>Healthy</b>: All health checks passed</description></item>
    /// <item><description><b>Degraded</b>: Some checks reported warnings but system is operational</description></item>
    /// <item><description><b>Unhealthy</b>: One or more critical checks failed</description></item>
    /// </list>
    /// 
    /// The response is formatted with indentation for improved readability when viewing
    /// health check results in browsers or command-line tools.
    /// </remarks>
    public static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        // Set response content type to JSON
        context.Response.ContentType = "application/json";

        // Build the health check response payload
        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds
            }),
            traceId = context.TraceIdentifier
        };

        // Serialize with indentation for human readability
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Write the JSON response
        await context.Response.WriteAsync(json);
    }
}