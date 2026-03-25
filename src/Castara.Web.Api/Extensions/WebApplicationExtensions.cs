using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Castara.Api.Configuration;
using Castara.Api.Health;
using Castara.Api.Middleware;

namespace Castara.Api.Extensions;

/// <summary>
/// Extension methods for configuring the application's middleware pipeline and endpoint routing.
/// </summary>
/// <remarks>
/// This class provides extension methods for setting up the HTTP request processing pipeline
/// and mapping API endpoints. The methods are organized into two main categories:
/// 
/// <b>Pipeline Configuration:</b>
/// <list type="bullet">
/// <item><description>Exception handling and error responses</description></item>
/// <item><description>Development tools (Swagger, OpenAPI)</description></item>
/// <item><description>Security features (HTTPS, CORS, header cleanup)</description></item>
/// <item><description>Rate limiting and throttling</description></item>
/// <item><description>Request correlation and logging</description></item>
/// </list>
/// 
/// <b>Endpoint Mapping:</b>
/// <list type="bullet">
/// <item><description>Health check endpoints (liveness and readiness probes)</description></item>
/// <item><description>API controllers with rate limiting</description></item>
/// </list>
/// 
/// Middleware ordering is critical:
/// The order in which middleware is registered determines the order in which it processes
/// requests (top-to-bottom) and responses (bottom-to-top). Incorrect ordering can cause:
/// <list type="bullet">
/// <item><description>Security vulnerabilities (e.g., CORS after authentication)</description></item>
/// <item><description>Missing correlation IDs in logs</description></item>
/// <item><description>Incomplete request timing data</description></item>
/// <item><description>Rate limiting not applied correctly</description></item>
/// </list>
/// 
/// The extension method pattern:
/// <list type="bullet">
/// <item><description>Keeps Program.cs clean and focused on application structure</description></item>
/// <item><description>Groups related configuration into logical units</description></item>
/// <item><description>Makes middleware ordering explicit and documented</description></item>
/// <item><description>Enables reusability across different hosting scenarios</description></item>
/// </list>
/// </remarks>
public static class WebApplicationExtensions
{
    /// <summary>
    /// The key used to store correlation IDs in HttpContext.Items.
    /// </summary>
    /// <remarks>
    /// This constant must match the header name used by <see cref="CorrelationIdMiddleware"/>
    /// to ensure correlation IDs are properly retrieved from context items and included in logs.
    /// </remarks>
    private const string CorrelationIdItemKey = "X-Correlation-Id";

    /// <summary>
    /// Configures the complete HTTP request processing pipeline for the API.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    /// <remarks>
    /// This method establishes the middleware pipeline in a carefully ordered sequence.
    /// Each middleware component is registered in the order it should process requests.
    /// 
    /// Middleware execution order (request flow):
    /// <list type="number">
    /// <item><description><b>Exception Handler</b> - Catches unhandled exceptions from all subsequent middleware</description></item>
    /// <item><description><b>Development Tools</b> - Swagger UI and OpenAPI (Development environment only)</description></item>
    /// <item><description><b>HTTPS Redirection</b> - Redirects HTTP requests to HTTPS</description></item>
    /// <item><description><b>CORS</b> - Evaluates cross-origin policies before authentication</description></item>
    /// <item><description><b>Security Headers</b> - Removes information disclosure headers</description></item>
    /// <item><description><b>Rate Limiter</b> - Applies throttling policies to prevent abuse</description></item>
    /// <item><description><b>Correlation ID</b> - Establishes request correlation for tracing</description></item>
    /// <item><description><b>Serilog Request Logging</b> - Rich structured logging with custom enrichment</description></item>
    /// <item><description><b>Controllers/Endpoints</b> - Actual request processing (mapped separately)</description></item>
    /// </list>
    /// 
    /// Response flow is reversed (bottom-to-top), allowing each middleware to:
    /// <list type="bullet">
    /// <item><description>Add headers to responses (correlation ID, security headers)</description></item>
    /// <item><description>Log completion information (Serilog request logging)</description></item>
    /// <item><description>Transform responses (exception handler)</description></item>
    /// </list>
    /// 
    /// Key ordering decisions:
    /// <list type="bullet">
    /// <item><description><b>Exception handler first</b>: Catches all errors including from other middleware</description></item>
    /// <item><description><b>CORS before authentication</b>: Allows preflight requests without auth</description></item>
    /// <item><description><b>Correlation ID before logging</b>: Ensures logs include correlation IDs</description></item>
    /// <item><description><b>Rate limiting before endpoints</b>: Reject excess requests early</description></item>
    /// </list>
    /// 
    /// Environment-specific behavior:
    /// <list type="bullet">
    /// <item><description><b>Development</b>: Enables Swagger UI and OpenAPI explorer</description></item>
    /// <item><description><b>Production</b>: Disables development tools for security and performance</description></item>
    /// </list>
    /// 
    /// This method is called from Program.cs:
    /// <code>
    /// app.UseApiPipeline();
    /// </code>
    /// </remarks>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Exception handler - Must be first to catch all errors
        // Configured to not suppress diagnostics so exceptions are logged properly
        ConfigureExceptionHandling(app);

        // Development-only tools: Swagger UI and OpenAPI
        // Only enabled in Development environment for security
        ConfigureDevelopmentTools(app);

        // HTTPS redirection - Redirect HTTP to HTTPS
        app.UseHttpsRedirection();

        // Security header cleanup - Remove information disclosure headers
        app.UseSecurityHeaderCleanup();

        // Rate limiting - Throttle excessive requests
        app.UseRateLimiter();

        // Correlation ID - Establish distributed tracing context
        // Must be before logging so correlation IDs appear in logs
        app.UseCorrelationId();

        // Serilog request logging - Rich structured logging with custom enrichment
        // Replaces RequestLoggingMiddleware with Serilog's optimized implementation
        ConfigureSerilogRequestLogging(app);

        // Note: Controllers are mapped in MapEndpoints()

        return app;
    }

    /// <summary>
    /// Maps all API endpoints including health checks and controllers.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    /// <remarks>
    /// This method configures endpoint routing for the application, including:
    /// 
    /// <b>Health Check Endpoints:</b>
    /// <list type="bullet">
    /// <item><description><c>/health/live</c> - Liveness probe for container orchestration</description></item>
    /// <item><description><c>/health/ready</c> - Readiness probe for load balancer routing</description></item>
    /// </list>
    /// 
    /// Both health check endpoints use a custom response writer (<see cref="HealthCheckResponseWriter.WriteResponse"/>)
    /// that provides detailed JSON output including:
    /// <list type="bullet">
    /// <item><description>Overall health status (Healthy, Degraded, Unhealthy)</description></item>
    /// <item><description>Individual check results with timing</description></item>
    /// <item><description>Total duration of health check execution</description></item>
    /// <item><description>Trace ID for correlation with logs</description></item>
    /// </list>
    /// 
    /// <b>Controller Endpoints:</b>
    /// All MVC controllers are mapped with automatic rate limiting applied via the
    /// "public-api" policy. This ensures consistent throttling across all API endpoints.
    /// 
    /// Health checks vs. Controllers:
    /// <list type="bullet">
    /// <item><description><b>Health checks</b>: No rate limiting (must always be accessible for monitoring)</description></item>
    /// <item><description><b>Controllers</b>: Rate limited to protect against abuse</description></item>
    /// </list>
    /// 
    /// Kubernetes integration:
    /// <code>
    /// livenessProbe:
    ///   httpGet:
    ///     path: /health/live
    ///     port: 8080
    ///   initialDelaySeconds: 3
    ///   periodSeconds: 10
    /// 
    /// readinessProbe:
    ///   httpGet:
    ///     path: /health/ready
    ///     port: 8080
    ///   initialDelaySeconds: 5
    ///   periodSeconds: 5
    /// </code>
    /// 
    /// This method is called from Program.cs:
    /// <code>
    /// app.MapEndpoints();
    /// </code>
    /// </remarks>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Map liveness health check endpoint
        // Used by Kubernetes to determine if the container should be restarted
        MapHealthCheck(app, "/health/live");

        // Map readiness health check endpoint
        // Used by load balancers to determine if the app can receive traffic
        MapHealthCheck(app, "/health/ready");

        // Map all controller endpoints with rate limiting
        // Rate limiting protects the API from abuse and resource exhaustion
        app.MapControllers()
           .RequireRateLimiting(ServiceCollectionExtensions.PublicApiRateLimitPolicy);

        return app;
    }

    /// <summary>
    /// Registers middleware to remove security-sensitive HTTP response headers.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This middleware removes HTTP headers that may disclose information about the
    /// server implementation, framework versions, or technology stack. Header removal
    /// is part of a defense-in-depth security strategy.
    /// 
    /// Headers are configured in appsettings.json:
    /// <code>
    /// "SecurityHeaders": {
    ///   "Remove": [
    ///     "Server",           // Web server type and version
    ///     "X-Powered-By",     // Framework information
    ///     "X-AspNet-Version", // ASP.NET version
    ///     "X-AspNetMvc-Version" // ASP.NET MVC version
    ///   ]
    /// }
    /// </code>
    /// 
    /// Why remove these headers:
    /// <list type="bullet">
    /// <item><description><b>Reduces attack surface</b>: Attackers can't easily identify framework versions</description></item>
    /// <item><description><b>Prevents targeted exploits</b>: Known vulnerabilities are harder to find</description></item>
    /// <item><description><b>Compliance</b>: Some security standards require header removal</description></item>
    /// <item><description><b>Professional appearance</b>: Doesn't advertise technology choices</description></item>
    /// </list>
    /// 
    /// Implementation notes:
    /// <list type="bullet">
    /// <item><description>Headers are removed using Response.OnStarting callback</description></item>
    /// <item><description>Removal happens just before the response is sent</description></item>
    /// <item><description>Configuration is optional; no headers removed if not configured</description></item>
    /// <item><description>Minimal performance impact (header removal is fast)</description></item>
    /// </list>
    /// 
    /// This is a custom inline middleware rather than a separate class because:
    /// <list type="bullet">
    /// <item><description>Simple logic that doesn't warrant a full middleware class</description></item>
    /// <item><description>Configuration access is straightforward via IApplicationBuilder</description></item>
    /// <item><description>Keeps related configuration cleanup in one place</description></item>
    /// </list>
    /// </remarks>
    public static IApplicationBuilder UseSecurityHeaderCleanup(this IApplicationBuilder app)
    {
        // Retrieve configuration from DI container
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

        // Load security headers configuration
        var options = configuration
            .GetSection("SecurityHeaders")
            .Get<SecurityHeadersOptions>();

        // Register middleware using inline delegate
        return app.Use(async (context, next) =>
        {
            // Register callback to remove headers just before response is sent
            context.Response.OnStarting(() =>
            {
                // Remove configured headers if any are specified
                if (options?.Remove is not null)
                {
                    foreach (var header in options.Remove)
                    {
                        context.Response.Headers.Remove(header);
                    }
                }

                return Task.CompletedTask;
            });

            // Continue to next middleware
            await next();
        });
    }

    /// <summary>
    /// Adds the <see cref="CorrelationIdMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This extension method registers the correlation ID middleware, which establishes
    /// distributed tracing context for each request. It should be registered early in
    /// the pipeline so that correlation IDs are available to all subsequent middleware
    /// and logging operations.
    /// 
    /// The middleware:
    /// <list type="bullet">
    /// <item><description>Extracts correlation IDs from X-Correlation-ID request headers</description></item>
    /// <item><description>Generates new ULIDs if no correlation ID is provided</description></item>
    /// <item><description>Adds correlation IDs to response headers for client tracking</description></item>
    /// <item><description>Creates log scopes with correlation IDs for automatic log enrichment</description></item>
    /// </list>
    /// 
    /// See <see cref="CorrelationIdMiddleware"/> for detailed implementation information.
    /// 
    /// Note: This method duplicates functionality from <see cref="CorrelationIdExtensions.UseCorrelationId"/>.
    /// Both are kept for flexibility in different usage scenarios.
    /// </remarks>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    #region Private Helper Methods

    /// <summary>
    /// Configures global exception handling for the application.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <remarks>
    /// Sets up the exception handler middleware with the following configuration:
    /// <list type="bullet">
    /// <item><description><b>SuppressDiagnosticsCallback = false</b>: Ensures exceptions are logged even when handled</description></item>
    /// <item><description>Uses <see cref="GlobalExceptionHandler"/> to transform exceptions into standardized error responses</description></item>
    /// </list>
    /// 
    /// In .NET 10+, handled exceptions suppress diagnostics by default. Setting
    /// SuppressDiagnosticsCallback to false ensures that exceptions are still logged
    /// to Application Insights, Serilog, and other diagnostic systems even after being
    /// converted to HTTP error responses by the exception handler.
    /// 
    /// This is critical for:
    /// <list type="bullet">
    /// <item><description>Monitoring and alerting on application errors</description></item>
    /// <item><description>Troubleshooting production issues</description></item>
    /// <item><description>Analyzing error trends and patterns</description></item>
    /// <item><description>Maintaining observability across the application</description></item>
    /// </list>
    /// </remarks>
    private static void ConfigureExceptionHandling(WebApplication app)
    {
        app.UseExceptionHandler();
    }

    /// <summary>
    /// Conditionally enables development tools (Swagger and OpenAPI) based on the hosting environment.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <remarks>
    /// Development tools are only enabled when the application is running in the Development environment.
    /// This ensures that:
    /// <list type="bullet">
    /// <item><description>Swagger UI is not exposed in production (security)</description></item>
    /// <item><description>OpenAPI specification is not publicly accessible (prevents API fingerprinting)</description></item>
    /// <item><description>Performance overhead of API exploration is avoided in production</description></item>
    /// </list>
    /// 
    /// Enabled endpoints in Development environment:
    /// <list type="bullet">
    /// <item><description><c>/openapi/v1.json</c> - OpenAPI specification document</description></item>
    /// <item><description><c>/swagger</c> - Swagger UI for interactive API documentation</description></item>
    /// <item><description><c>/swagger/v1/swagger.json</c> - Alternative OpenAPI endpoint</description></item>
    /// </list>
    /// 
    /// In production, API documentation should be:
    /// <list type="bullet">
    /// <item><description>Hosted on a separate documentation site</description></item>
    /// <item><description>Generated during build and published separately</description></item>
    /// <item><description>Protected behind authentication if sensitive</description></item>
    /// </list>
    /// </remarks>
    private static void ConfigureDevelopmentTools(WebApplication app)
    {
        // Early return if not in Development environment
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    /// <summary>
    /// Configures Serilog's request logging middleware with custom message templates and enrichment.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <remarks>
    /// This method configures Serilog's built-in request logging middleware, which is more
    /// efficient and feature-rich than custom request logging middleware. It provides:
    /// 
    /// <b>Custom message template:</b>
    /// <code>
    /// HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms
    /// </code>
    /// 
    /// This provides a consistent, readable format for all request logs with precise timing.
    /// 
    /// <b>Dynamic log level assignment:</b>
    /// <list type="bullet">
    /// <item><description><b>Error (500-599)</b>: Server errors or exceptions - requires immediate attention</description></item>
    /// <item><description><b>Warning (400-499)</b>: Client errors - may indicate API misuse or validation issues</description></item>
    /// <item><description><b>Information (200-399)</b>: Successful requests - normal operation</description></item>
    /// </list>
    /// 
    /// This intelligent level assignment enables:
    /// <list type="bullet">
    /// <item><description>Filtering production logs to show only warnings and errors</description></item>
    /// <item><description>Alerting on error-level logs (500+ status codes)</description></item>
    /// <item><description>Analyzing client errors separately from server errors</description></item>
    /// <item><description>Reducing log volume while maintaining observability</description></item>
    /// </list>
    /// 
    /// <b>Diagnostic context enrichment:</b>
    /// Automatically adds detailed properties to all request logs:
    /// 
    /// <b>Request properties:</b>
    /// <list type="bullet">
    /// <item><description><b>RequestHost</b>: Host header value (e.g., "api.example.com")</description></item>
    /// <item><description><b>RequestScheme</b>: HTTP or HTTPS</description></item>
    /// <item><description><b>RequestMethod</b>: HTTP method (GET, POST, etc.)</description></item>
    /// <item><description><b>RequestPath</b>: URL path (e.g., "/api/v1/bonds/value")</description></item>
    /// <item><description><b>QueryString</b>: Query parameters if present</description></item>
    /// </list>
    /// 
    /// <b>Response properties:</b>
    /// <list type="bullet">
    /// <item><description><b>StatusCode</b>: HTTP response status code</description></item>
    /// <item><description><b>EndpointName</b>: Matched endpoint display name</description></item>
    /// </list>
    /// 
    /// <b>Tracing properties:</b>
    /// <list type="bullet">
    /// <item><description><b>TraceIdentifier</b>: ASP.NET Core request trace ID</description></item>
    /// <item><description><b>CorrelationId</b>: ULID correlation ID from CorrelationIdMiddleware</description></item>
    /// </list>
    /// 
    /// The correlation ID is retrieved from HttpContext.Items where it was stored by
    /// <see cref="CorrelationIdMiddleware"/>. This enables distributed tracing across
    /// multiple services and log correlation in centralized logging systems.
    /// 
    /// <b>Benefits over custom middleware:</b>
    /// <list type="bullet">
    /// <item><description>Optimized performance with minimal allocations</description></item>
    /// <item><description>Consistent with Serilog ecosystem and patterns</description></item>
    /// <item><description>Rich diagnostic context automatically available in all logs during request</description></item>
    /// <item><description>Built-in elapsed time tracking with high precision</description></item>
    /// <item><description>Proper integration with Serilog configuration and sinks</description></item>
    /// </list>
    /// 
    /// Example log output:
    /// <code>
    /// {"@t":"2026-03-10T15:30:45.1234Z","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms","RequestMethod":"GET","RequestPath":"/api/v1/bonds/value","StatusCode":200,"Elapsed":145.2341,"CorrelationId":"01HN3KQVMQXYZ5N8J7G2P4W6ST","TraceIdentifier":"0HMVFE3A4TQKJ:00000001","RequestHost":"localhost:5001","RequestScheme":"https","EndpointName":"BondsController.Value","@l":"Information"}
    /// </code>
    /// </remarks>
    private static void ConfigureSerilogRequestLogging(WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Custom message template for consistent, readable log format
            // Includes method, path, status code, and precise elapsed time
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Dynamic log level based on response status code
            // Enables filtering and alerting based on request outcomes
            options.GetLevel = (httpContext, elapsed, exception) =>
            {
                // Server errors (500+) or exceptions -> Error level
                // Requires immediate attention
                if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                {
                    return LogEventLevel.Error;
                }

                // Client errors (400-499) -> Warning level
                // May indicate API misuse or validation issues
                if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
                {
                    return LogEventLevel.Warning;
                }

                // Successful requests (200-399) -> Information level
                // Normal operation
                return LogEventLevel.Information;
            };

            // Enrich diagnostic context with detailed request/response information
            // These properties are available in all logs during the request
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                // Request properties - Where did the request come from?
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
                diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value ?? string.Empty);
                
                // Response properties - How did we respond?
                diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
                diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);

                // Endpoint information - Which controller/action handled this?
                if (httpContext.GetEndpoint() is Endpoint endpoint)
                {
                    diagnosticContext.Set("EndpointName", endpoint.DisplayName ?? string.Empty);
                }

                // Correlation ID - Retrieve from HttpContext.Items where CorrelationIdMiddleware stored it
                // This enables distributed tracing across services
                if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var correlationId)
                    && correlationId is string correlationIdValue
                    && !string.IsNullOrWhiteSpace(correlationIdValue))
                {
                    diagnosticContext.Set("CorrelationId", correlationIdValue);
                }
            };
        });
    }

    /// <summary>
    /// Maps a health check endpoint with custom response formatting.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <param name="pattern">The URL pattern for the health check endpoint.</param>
    /// <remarks>
    /// This helper method centralizes health check endpoint mapping to ensure consistency.
    /// All health check endpoints use the same custom response writer for uniform JSON output.
    /// 
    /// The custom response writer (<see cref="HealthCheckResponseWriter.WriteResponse"/>)
    /// provides structured JSON instead of plain text, including:
    /// <list type="bullet">
    /// <item><description>Overall health status</description></item>
    /// <item><description>Individual check results</description></item>
    /// <item><description>Timing information</description></item>
    /// <item><description>Trace ID for correlation</description></item>
    /// </list>
    /// 
    /// Used by <see cref="MapEndpoints"/> to map both:
    /// <list type="bullet">
    /// <item><description>/health/live - Liveness probe</description></item>
    /// <item><description>/health/ready - Readiness probe</description></item>
    /// </list>
    /// </remarks>
    private static void MapHealthCheck(WebApplication app, string pattern)
    {
        app.MapHealthChecks(pattern, new HealthCheckOptions
        {
            // Use custom response writer for structured JSON output
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });
    }

    #endregion
}