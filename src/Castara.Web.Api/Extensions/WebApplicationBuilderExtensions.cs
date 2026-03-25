using Serilog;

namespace Castara.Api.Extensions;

/// <summary>
/// Extension methods for configuring the <see cref="WebApplicationBuilder"/> during application startup.
/// </summary>
/// <remarks>
/// This class provides extension methods for configuring cross-cutting concerns that need to be
/// set up before the application is built, such as logging infrastructure.
/// 
/// Configuration areas:
/// <list type="bullet">
/// <item><description>Structured logging with Serilog</description></item>
/// <item><description>Configuration-driven log setup from appsettings.json</description></item>
/// <item><description>Log enrichment with contextual properties</description></item>
/// </list>
/// 
/// These extensions complement <see cref="ServiceCollectionExtensions"/> (service registration)
/// and <see cref="WebApplicationExtensions"/> (middleware pipeline configuration) to provide
/// a complete application configuration strategy.
/// </remarks>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures structured logging using Serilog with configuration-driven setup.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The web application builder for method chaining.</returns>
    /// <remarks>
    /// This method sets up Serilog as the logging provider for the application, replacing the
    /// default ASP.NET Core logging infrastructure. The configuration is driven primarily by
    /// appsettings.json, making it easy to adjust logging behavior without code changes.
    /// 
    /// <b>Why Serilog:</b>
    /// <list type="bullet">
    /// <item><description><b>Structured logging</b>: Logs are structured data objects, not strings</description></item>
    /// <item><description><b>Rich enrichment</b>: Add contextual properties to all log entries</description></item>
    /// <item><description><b>Flexible sinks</b>: Write to multiple destinations (console, files, Application Insights)</description></item>
    /// <item><description><b>Configuration-driven</b>: Change log levels and sinks via appsettings.json</description></item>
    /// <item><description><b>Easy querying</b>: Structured logs are easy to filter and analyze</description></item>
    /// </list>
    /// 
    /// <b>Configuration approach:</b>
    /// This method uses a configuration-first approach with three configuration sources:
    /// <list type="number">
    /// <item><description><b>ReadFrom.Configuration</b>: Loads settings from appsettings.json (log levels, sinks, enrichers)</description></item>
    /// <item><description><b>ReadFrom.Services</b>: Allows Serilog to access registered services (for custom enrichers/sinks)</description></item>
    /// <item><description><b>Enrich.FromLogContext</b>: Captures properties from log scopes (middleware enrichment)</description></item>
    /// </list>
    /// 
    /// <b>Example appsettings.json configuration:</b>
    /// <code>
    /// "Serilog": {
    ///   "Using": ["Serilog.Sinks.Console"],
    ///   "MinimumLevel": {
    ///     "Default": "Information",
    ///     "Override": {
    ///       "Microsoft.AspNetCore": "Warning",
    ///       "System": "Warning"
    ///     }
    ///   },
    ///   "WriteTo": [
    ///     {
    ///       "Name": "Console",
    ///       "Args": {
    ///         "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact"
    ///       }
    ///     }
    ///   ],
    ///   "Enrich": ["FromLogContext", "WithThreadId", "WithMachineName"],
    ///   "Properties": {
    ///     "Application": "Castara.Api"
    ///   }
    /// }
    /// </code>
    /// 
    /// <b>Log enrichment from middleware:</b>
    /// The <c>FromLogContext</c> enrichment captures properties added by middleware using <c>BeginScope</c>:
    /// <list type="bullet">
    /// <item><description><b>CorrelationId</b>: Distributed tracing identifier (from CorrelationIdMiddleware)</description></item>
    /// <item><description><b>TraceId</b>: ASP.NET Core request identifier (from middleware)</description></item>
    /// <item><description><b>Method</b>: HTTP method - GET, POST, etc. (from RequestLoggingMiddleware)</description></item>
    /// <item><description><b>Path</b>: Request path (from RequestLoggingMiddleware)</description></item>
    /// </list>
    /// 
    /// <b>Additional code-based enrichment:</b>
    /// <list type="bullet">
    /// <item><description><b>Environment</b>: Development/Production/Testing - Current hosting environment</description></item>
    /// </list>
    /// 
    /// <b>Benefits of configuration-driven approach:</b>
    /// <list type="bullet">
    /// <item><description><b>No recompilation</b>: Change log levels and sinks by editing appsettings.json</description></item>
    /// <item><description><b>Environment-specific</b>: Use appsettings.Development.json vs appsettings.Production.json</description></item>
    /// <item><description><b>Flexibility</b>: Add new enrichers and sinks without code changes</description></item>
    /// <item><description><b>Standardization</b>: All logging configuration in one place</description></item>
    /// <item><description><b>Service integration</b>: ReadFrom.Services enables dependency injection in Serilog</description></item>
    /// </list>
    /// 
    /// <b>Example appsettings.Development.json override:</b>
    /// <code>
    /// "Serilog": {
    ///   "MinimumLevel": {
    ///     "Default": "Debug",
    ///     "Override": {
    ///       "Microsoft.AspNetCore": "Information"
    ///     }
    ///   }
    /// }
    /// </code>
    /// 
    /// <b>Example log output (with RenderedCompactJsonFormatter):</b>
    /// <code>
    /// {"@t":"2026-03-10T15:30:45.1234567Z","@mt":"Incoming request {Method} {Path}","Method":"GET","Path":"/api/v1/bonds/value","Application":"Castara.Api","Environment":"Production","CorrelationId":"01HN3KQVMQXYZ5N8J7G2P4W6ST","TraceId":"0HMVFE3A4TQKJ:00000001"}
    /// </code>
    /// 
    /// <b>Integration scenarios:</b>
    /// <list type="bullet">
    /// <item><description><b>Local Development</b>: Console output with structured JSON (configured in appsettings.Development.json)</description></item>
    /// <item><description><b>Docker/Kubernetes</b>: Container stdout captured by orchestrator</description></item>
    /// <item><description><b>Azure App Service</b>: Add Application Insights sink in appsettings.json</description></item>
    /// <item><description><b>Production</b>: Multiple sinks (console, file, Application Insights) configured per environment</description></item>
    /// </list>
    /// 
    /// <b>Performance considerations:</b>
    /// <list type="bullet">
    /// <item><description>Serilog uses async I/O for non-blocking writes</description></item>
    /// <item><description>Log level filtering happens before message formatting</description></item>
    /// <item><description>FromLogContext has minimal overhead (property attachment)</description></item>
    /// <item><description>Typical overhead: 5-10µs per log entry</description></item>
    /// </list>
    /// 
    /// This method is called from Program.cs before service registration:
    /// <code>
    /// builder.ConfigureLogging();
    /// </code>
    /// 
    /// This ensures logging is available during application startup and service registration,
    /// allowing early-stage errors to be logged properly.
    /// </remarks>
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Configure Serilog using the host builder's integrated approach
        // This provides access to configuration, services, and host context
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                // Read Serilog configuration from appsettings.json
                // This includes: minimum log levels, sinks, enrichers, and properties
                // Configuration section: "Serilog"
                .ReadFrom.Configuration(context.Configuration)
                
                // Enable Serilog to access registered services for dependency injection
                // Allows custom enrichers and sinks to resolve services from DI container
                .ReadFrom.Services(services)
                
                // Enrich logs with properties from log scopes (BeginScope)
                // Captures CorrelationId, TraceId, Method, Path from middleware
                // Essential for distributed tracing and request correlation
                .Enrich.FromLogContext()
                
                // Add hosting environment name (Development, Production, Testing) to all logs
                // Essential for filtering logs by environment in centralized logging systems
                // Helps distinguish logs when multiple environments write to the same destination
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });

        return builder;
    }
}