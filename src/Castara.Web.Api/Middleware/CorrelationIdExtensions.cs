namespace Castara.Api.Middleware;

/// <summary>
/// Extension methods for registering the <see cref="CorrelationIdMiddleware"/> in the application pipeline.
/// </summary>
/// <remarks>
/// This class provides a fluent API for adding the correlation ID middleware to the ASP.NET Core
/// pipeline. Extension methods are a standard pattern in ASP.NET Core for making middleware
/// registration more readable and discoverable.
/// 
/// Benefits of using extension methods for middleware:
/// <list type="bullet">
/// <item><description>Cleaner and more expressive syntax in Program.cs</description></item>
/// <item><description>Consistent with built-in ASP.NET Core middleware registration patterns</description></item>
/// <item><description>IntelliSense discoverability when typing "app.Use..."</description></item>
/// <item><description>Encapsulates the middleware type name and registration details</description></item>
/// </list>
/// 
/// Instead of writing:
/// <code>
/// app.UseMiddleware&lt;CorrelationIdMiddleware&gt;();
/// </code>
/// 
/// You can write the more intuitive:
/// <code>
/// app.UseCorrelationId();
/// </code>
/// 
/// This follows the same pattern as built-in methods like <c>UseAuthentication()</c>,
/// <c>UseAuthorization()</c>, and <c>UseRouting()</c>.
/// </remarks>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds the <see cref="CorrelationIdMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This extension method registers the correlation ID middleware, which:
    /// <list type="bullet">
    /// <item><description>Extracts or generates correlation IDs for distributed tracing</description></item>
    /// <item><description>Propagates correlation IDs across service boundaries via X-Correlation-ID header</description></item>
    /// <item><description>Stores correlation IDs in HttpContext.Items for access by other components</description></item>
    /// <item><description>Adds correlation IDs to response headers for client tracking</description></item>
    /// <item><description>Creates log scopes with correlation IDs for automatic log enrichment</description></item>
    /// </list>
    /// 
    /// The middleware should be registered very early in the pipeline, typically as one of the
    /// first middleware components. This ensures that the correlation ID is available to all
    /// subsequent middleware, controllers, services, and logging operations throughout the
    /// request lifecycle.
    /// 
    /// Recommended pipeline placement:
    /// <list type="number">
    /// <item><description>Exception handling middleware</description></item>
    /// <item><description>Correlation ID middleware (this middleware)</description></item>
    /// <item><description>Request logging middleware</description></item>
    /// <item><description>Authentication/Authorization</description></item>
    /// <item><description>Routing and endpoints</description></item>
    /// </list>
    /// 
    /// The method returns the <see cref="IApplicationBuilder"/> to enable method chaining,
    /// allowing you to continue configuring the pipeline in a fluent style.
    /// </remarks>
    /// <example>
    /// Register the middleware in Program.cs:
    /// <code>
    /// var app = builder.Build();
    /// 
    /// app.UseExceptionHandler(...);
    /// app.UseCorrelationId();      // Register correlation ID early
    /// app.UseRequestLogging();     // Request logging can now access correlation ID
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.MapControllers();
    /// </code>
    /// 
    /// The correlation ID will be available in logs:
    /// <code>
    /// // Logs will automatically include:
    /// // [INF] {CorrelationId=01HN3KQVMQXYZ5N8J7G2P4W6ST} Processing request...
    /// </code>
    /// </example>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}