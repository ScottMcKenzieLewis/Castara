using Castara.Api.Configuration;
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

    /// <summary>
    /// Configures the Kestrel web server's request headers timeout to prevent slow header attacks.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The web application builder for method chaining.</returns>
    /// <remarks>
    /// This method configures the maximum time allowed for receiving HTTP request headers from clients.
    /// Setting a timeout helps protect the server from:
    /// <list type="bullet">
    /// <item><description><b>Slowloris attacks</b>: Malicious clients sending headers very slowly to exhaust server resources</description></item>
    /// <item><description><b>Connection exhaustion</b>: Slow clients keeping connections open indefinitely</description></item>
    /// <item><description><b>Network issues</b>: Hung or misbehaving clients with poor connectivity</description></item>
    /// <item><description><b>Resource leaks</b>: Connections waiting forever for complete headers</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Configuration:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Timeout</b>: 10 seconds to receive all request headers</description></item>
    /// <item><description><b>Scope</b>: Applies to all HTTP/1.1 and HTTP/2 requests</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Behavior when timeout exceeded:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Kestrel closes the connection immediately</description></item>
    /// <item><description>No HTTP response is sent (connection is terminated)</description></item>
    /// <item><description>Connection resources are immediately released</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Typical header sizes and times:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Normal request headers: 0.5-2 KB (milliseconds to transmit)</description></item>
    /// <item><description>Crash report request headers: 2-4 KB with HMAC signature (still milliseconds)</description></item>
    /// <item><description>10-second timeout provides significant margin for slow networks</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Why 10 seconds:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Generous allowance for slow mobile/satellite connections</description></item>
    /// <item><description>Prevents legitimate requests from timing out</description></item>
    /// <item><description>Short enough to quickly reject malicious slow-header attacks</description></item>
    /// <item><description>Matches industry best practices (5-30 seconds typical range)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Relationship to request timeout:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Request headers timeout</b>: Time to receive headers (this setting)</description></item>
    /// <item><description><b>Request timeout</b>: Time to process entire request including body (configured in <see cref="ServiceCollectionExtensions.ConfigureRequestTimeouts"/>)</description></item>
    /// </list>
    /// 
    /// This method is called from Program.cs before building the application:
    /// <code>
    /// builder.ConfigureRequestHeadersTimeout();
    /// </code>
    /// </remarks>
    public static WebApplicationBuilder ConfigureRequestHeadersTimeout(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Set maximum time to receive request headers to 10 seconds
            // Protects against slowloris attacks and connection exhaustion
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
        });

        return builder;
    }

    /// <summary>
    /// Registers and binds crash report ingestion configuration options from appsettings.json.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The web application builder for method chaining.</returns>
    /// <remarks>
    /// This method configures the <see cref="CrashReportIngestionOptions"/> by binding values from
    /// the "CrashReportIngestion" section of appsettings.json, making them available throughout
    /// the application via dependency injection.
    /// 
    /// <para>
    /// <b>Configuration structure (appsettings.json):</b>
    /// </para>
    /// <code>
    /// "CrashReportIngestion": {
    ///   "Enabled": true,
    ///   "AllowedClockSkewMinutes": 5,
    ///   "HmacKeys": {
    ///     "castara-wpf-v1": "your-secret-key-here-min-32-chars"
    ///   }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Configuration properties:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Enabled (bool)</term>
    /// <description>Master switch for crash report ingestion. When false, all crash report submissions
    /// receive HTTP 503 (Service Unavailable). Useful for maintenance windows or disabling the feature.</description>
    /// </item>
    /// <item>
    /// <term>AllowedClockSkewMinutes (int)</term>
    /// <description>Maximum allowed clock skew for HMAC signature timestamp validation (typically 5 minutes).
    /// Prevents replay attacks while accommodating minor clock differences between client and server.</description>
    /// </item>
    /// <item>
    /// <term>HmacKeys (Dictionary&lt;string, string&gt;)</term>
    /// <description>Mapping of key IDs to shared secrets for HMAC-SHA256 signature validation.
    /// Supports key rotation by maintaining multiple active keys. Key IDs are sent in the
    /// X-Castara-Key-Id header by clients.</description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>Usage in services:</b>
    /// </para>
    /// <code>
    /// public class CrashReportHmacValidationMiddleware
    /// {
    ///     public CrashReportHmacValidationMiddleware(
    ///         IOptions&lt;CrashReportIngestionOptions&gt; options)
    ///     {
    ///         var config = options.Value;
    ///         if (!config.Enabled) { /* reject request */ }
    ///         var secret = config.HmacKeys[keyId];
    ///     }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Security considerations:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Secret storage</b>: Store HMAC keys in Azure Key Vault or AWS Secrets Manager in production</description></item>
    /// <item><description><b>Key rotation</b>: Add new keys before removing old ones to avoid downtime</description></item>
    /// <item><description><b>Key length</b>: Use minimum 32-character secrets (256 bits) for HMAC-SHA256</description></item>
    /// <item><description><b>Environment separation</b>: Use different keys for development, staging, and production</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Environment-specific configuration:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Development</b>: appsettings.Development.json - Simple keys, ingestion enabled</description></item>
    /// <item><description><b>Production</b>: Environment variables override appsettings.json - Secrets from Key Vault</description></item>
    /// <item><description><b>Testing</b>: appsettings.Testing.json - Ingestion disabled (use NullCrashReportStorageService)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Key rotation example:</b>
    /// </para>
    /// <code>
    /// // Step 1: Add new key while keeping old one
    /// "HmacKeys": {
    ///   "castara-wpf-v1": "old-secret-key",
    ///   "castara-wpf-v2": "new-secret-key"
    /// }
    /// 
    /// // Step 2: Update clients to use v2
    /// // Step 3: Remove v1 after grace period
    /// "HmacKeys": {
    ///   "castara-wpf-v2": "new-secret-key"
    /// }
    /// </code>
    /// 
    /// This method is called from Program.cs before service registration:
    /// <code>
    /// builder.AddCrashReportIngestionOptions();
    /// </code>
    /// 
    /// This ensures configuration is available to all services that depend on crash report settings.
    /// </remarks>
    public static WebApplicationBuilder AddCrashReportIngestionOptions(this WebApplicationBuilder builder)
    {
        // Bind the "CrashReportIngestion" configuration section to CrashReportIngestionOptions
        // Makes options available via IOptions<CrashReportIngestionOptions> in DI container
        builder.Services.Configure<CrashReportIngestionOptions>(
            builder.Configuration.GetSection(CrashReportIngestionOptions.SectionName));

        return builder;
    }

}
