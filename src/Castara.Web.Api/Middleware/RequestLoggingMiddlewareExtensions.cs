namespace Treasara.Api.Middleware;

/// <summary>
/// Extension methods for registering the <see cref="RequestLoggingMiddleware"/> in the application pipeline.
/// </summary>
/// <remarks>
/// This class provides a fluent API for adding the request logging middleware to the ASP.NET Core
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
/// app.UseMiddleware&lt;RequestLoggingMiddleware&gt;();
/// </code>
/// 
/// You can write the more intuitive:
/// <code>
/// app.UseRequestLogging();
/// </code>
/// 
/// This follows the same pattern as built-in methods like <c>UseAuthentication()</c>,
/// <c>UseAuthorization()</c>, and <c>UseRouting()</c>.
/// </remarks>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="RequestLoggingMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This extension method registers the request logging middleware, which logs:
    /// <list type="bullet">
    /// <item><description>Incoming HTTP request method and path</description></item>
    /// <item><description>Request completion status code</description></item>
    /// <item><description>Request processing duration in milliseconds</description></item>
    /// <item><description>Correlation information via trace IDs</description></item>
    /// </list>
    /// 
    /// The middleware should be registered early in the pipeline to capture timing
    /// information for all subsequent middleware and endpoint handlers. Typical placement
    /// is after exception handling but before authentication, authorization, and routing.
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
    /// app.UseRequestLogging();  // Register request logging
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.MapControllers();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}