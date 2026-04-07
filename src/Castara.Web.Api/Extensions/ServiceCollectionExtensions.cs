using Asp.Versioning;
using Castara.Api.Configuration;
using Castara.Api.Diagnostics.Services;
using Castara.Api.Exceptions;
using Castara.Api.OpenApi;
using Castara.Api.Serialization;
using Castara.Api.Services.Diagnostics;
using Castara.Diagnostics.Api.Services.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Validation;
using Castara.Web.Api.Services.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Threading.RateLimiting;

namespace Castara.Web.Api.Extensions;

/// <summary>
/// Extension methods for configuring application services in the dependency injection container.
/// </summary>
/// <remarks>
/// This class provides extension methods for registering all application services, including:
/// <list type="bullet">
/// <item><description>Controllers and MVC services</description></item>
/// <item><description>API versioning configuration</description></item>
/// <item><description>Rate limiting policies</description></item>
/// <item><description>OpenAPI/Swagger documentation</description></item>
/// <item><description>Application-specific services (mappers, validators, exception handlers)</description></item>
/// <item><description>Health check services</description></item>
/// <item><description>CORS policies</description></item>
/// <item><description>FluentValidation validators</description></item>
/// </list>
/// 
/// The extension method pattern provides several benefits:
/// <list type="bullet">
/// <item><description>Organizes related service registrations into logical groups</description></item>
/// <item><description>Keeps Program.cs clean and maintainable</description></item>
/// <item><description>Makes it easy to find and modify specific configuration areas</description></item>
/// <item><description>Enables reusability across different applications or test scenarios</description></item>
/// </list>
/// 
/// Service registration follows these principles:
/// <list type="bullet">
/// <item><description>Configuration-driven setup using appsettings.json</description></item>
/// <item><description>Appropriate service lifetimes (Singleton, Scoped, Transient)</description></item>
/// <item><description>Fail-fast with sensible defaults when configuration is missing</description></item>
/// <item><description>Clear separation between infrastructure and application services</description></item>
/// </list>
/// </remarks>
public static class ServiceCollectionExtensions
{

    /// <summary>
    /// The name of the rate limiting policy applied to public API endpoints.
    /// </summary>
    /// <remarks>
    /// This policy name is used when mapping endpoints with rate limiting requirements.
    /// The actual rate limits are configured in appsettings.json under "RateLimiting".
    /// </remarks>
    public const string PublicApiRateLimitPolicy = "public-api";

    /// <summary>
    /// Registers all services required by the API application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing settings from appsettings.json.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This is the main orchestration method that calls all other service registration methods
    /// in the appropriate order. It ensures that all dependencies are registered before they're needed.
    /// 
    /// Services are registered in the following order:
    /// <list type="number">
    /// <item><description>Controllers - ASP.NET Core MVC controllers</description></item>
    /// <item><description>API Versioning - URL-based versioning support</description></item>
    /// <item><description>Rate Limiting - Throttling policies for API protection</description></item>
    /// <item><description>OpenAPI - Swagger documentation generation</description></item>
    /// <item><description>Application Services - Domain-specific services and handlers</description></item>
    /// <item><description>Health Checks - Liveness and readiness probes</description></item>
    /// <item><description>CORS - Cross-origin resource sharing policies</description></item>
    /// <item><description>Validators - FluentValidation validators for request DTOs</description></item>
    /// </list>
    /// 
    /// This method is called from Program.cs:
    /// <code>
    /// builder.Services.AddApi(builder.Configuration);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register organized service groups
        services.AddApiVersioning();
        services.AddRateLimiting(configuration);
        services.ConfigureRequestTimeouts();
        services.AddOpenApi(typeof(Program).Assembly);
        services.AddApplicationServices();
        services.AddHealthChecks();
        services.AddValidators();
        services.AddJsonOptions();

        return services;
    }

    /// <summary>
    /// Configures API versioning using URL-based versioning (e.g., /api/v1/..., /api/v2/...).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// API versioning configuration:
    /// <list type="bullet">
    /// <item><description><b>Default version</b>: 1.0 - Used when clients don't specify a version</description></item>
    /// <item><description><b>Version reader</b>: UrlSegmentApiVersionReader - Reads version from URL path</description></item>
    /// <item><description><b>ReportApiVersions</b>: true - Adds API version headers to responses</description></item>
    /// <item><description><b>API Explorer</b>: Configured for Swagger integration with version substitution</description></item>
    /// </list>
    /// 
    /// URL format: <c>/api/v{version}/[controller]/[action]</c>
    /// 
    /// Examples:
    /// <list type="bullet">
    /// <item><description>/api/v1/bonds/value - Version 1.0 endpoint</description></item>
    /// <item><description>/api/v2/bonds/value - Version 2.0 endpoint (future)</description></item>
    /// </list>
    /// 
    /// The GroupNameFormat "'v'V" creates version groups like "v1", "v2" for Swagger documentation.
    /// </remarks>
    public static IServiceCollection AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Set default API version to 1.0
            options.DefaultApiVersion = new ApiVersion(1, 0);

            // Assume default version when client doesn't specify one
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Add "api-supported-versions" header to responses
            options.ReportApiVersions = true;

            // Read version from URL segment (e.g., /api/v1/...)
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Configures rate limiting policies to protect the API from excessive request rates.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing rate limiting settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Rate limiting helps protect the API from:
    /// <list type="bullet">
    /// <item><description>Denial-of-service (DoS) attacks</description></item>
    /// <item><description>Brute force authentication attempts</description></item>
    /// <item><description>Resource exhaustion from excessive requests</description></item>
    /// <item><description>Unintentional client errors causing request loops</description></item>
    /// </list>
    /// 
    /// Configuration is loaded from appsettings.json:
    /// <code>
    /// "RateLimiting": {
    ///   "PermitLimit": 30,      // Requests allowed per window
    ///   "WindowSeconds": 60,     // Time window in seconds
    ///   "QueueLimit": 0          // Max queued requests (0 = reject immediately)
    /// }
    /// </code>
    /// 
    /// The fixed window limiter:
    /// <list type="bullet">
    /// <item><description>Allows N requests per time window</description></item>
    /// <item><description>Returns HTTP 429 (Too Many Requests) when limit exceeded</description></item>
    /// <item><description>Processes queued requests oldest-first (FIFO)</description></item>
    /// <item><description>Resets at the end of each window</description></item>
    /// </list>
    /// 
    /// Default values (if configuration missing):
    /// <list type="bullet">
    /// <item><description>PermitLimit: 30 requests</description></item>
    /// <item><description>Window: 60 seconds</description></item>
    /// <item><description>QueueLimit: 0 (no queuing)</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind rate limiting configuration to strongly-typed options
        services.Configure<RateLimitingOptions>(
            configuration.GetSection("RateLimiting"));

        // Load configuration values for immediate use
        var config = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitingOptions>();

        services.AddRateLimiter(options =>
        {
            // Return HTTP 429 when rate limit exceeded
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Configure fixed window rate limiter for public API
            options.AddFixedWindowLimiter(PublicApiRateLimitPolicy, limiterOptions =>
            {
                // Maximum requests allowed per window (default: 30)
                limiterOptions.PermitLimit = config?.PermitLimit ?? 30;

                // Time window duration (default: 60 seconds)
                limiterOptions.Window = TimeSpan.FromSeconds(config?.WindowSeconds ?? 60);

                // Maximum requests to queue when limit exceeded (default: 0, reject immediately)
                limiterOptions.QueueLimit = config?.QueueLimit ?? 0;

                // Process queued requests in FIFO order
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        return services;
    }

    /// <summary>
    /// Configures OpenAPI (Swagger) documentation generation for the API.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="apiAssembly">The assembly containing the API controllers and XML documentation.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers services for OpenAPI/Swagger documentation:
    /// <list type="bullet">
    /// <item>
    /// <term>API Explorer</term>
    /// <description>Generates API metadata by discovering endpoints, parameters, and response types</description>
    /// </item>
    /// <item>
    /// <term>Swagger Options Configuration</term>
    /// <description><see cref="ConfigureSwaggerOptions"/> provides version-aware Swagger document configuration
    /// including title, description, and API version information</description>
    /// </item>
    /// <item>
    /// <term>Swagger Generator</term>
    /// <description>Creates OpenAPI 3.0 specification documents from API metadata with XML documentation comments</description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// <b>XML Documentation Integration:</b>
    /// </para>
    /// The method automatically includes XML documentation comments from the API assembly if available.
    /// This enriches the Swagger UI with detailed descriptions, parameter information, and example values
    /// from /// comments in controller classes and action methods.
    /// 
    /// <para>
    /// To enable XML documentation generation, ensure the project file includes:
    /// </para>
    /// <code>
    /// &lt;PropertyGroup&gt;
    ///   &lt;GenerateDocumentationFile&gt;true&lt;/GenerateDocumentationFile&gt;
    /// &lt;/PropertyGroup&gt;
    /// </code>
    /// 
    /// <para>
    /// <b>Swagger UI Endpoints:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>/swagger</c> - Interactive API documentation and testing interface</description></item>
    /// <item><description><c>/swagger/v1/swagger.json</c> - OpenAPI 3.0 specification document for API v1</description></item>
    /// <item><description><c>/swagger/v2/swagger.json</c> - OpenAPI specification for future API versions</description></item>
    /// </list>
    /// 
    /// The Swagger UI is mapped by the UseSwaggerDocumentation extension method
    /// and is only enabled in Development environment by default.
    /// </remarks>
    public static IServiceCollection AddOpenApi(this IServiceCollection services,
            Assembly apiAssembly)
    {
        // Register API Explorer for metadata generation
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        // Register Swagger documentation generator
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{apiAssembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers application-specific services including exception handlers, validators, 
    /// and crash report processing services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item>
    /// <term>Problem Details (Scoped)</term>
    /// <description>RFC 7807 problem details support for standardized error responses across the API</description>
    /// </item>
    /// <item>
    /// <term>Global Exception Handler (Scoped)</term>
    /// <description>Catches unhandled exceptions and converts them to RFC 7807 problem details responses</description>
    /// </item>
    /// <item>
    /// <term>Validation Error Response Factory (Scoped)</term>
    /// <description>Creates consistent validation error responses following RFC 7807 format</description>
    /// </item>
    /// <item>
    /// <term>Crash Report Request Signature Validator (Scoped)</term>
    /// <description>Validates HMAC-SHA256 signatures on crash report submissions to ensure authenticity</description>
    /// </item>
    /// <item>
    /// <term>Crash Report Sanitizer (Scoped)</term>
    /// <description>Server-side sanitization of crash reports to redact file paths and usernames (defense-in-depth)</description>
    /// </item>
    /// <item>
    /// <term>Crash Report Storage Service (Scoped)</term>
    /// <description>Default implementation is <see cref="NullCrashReportStorageService"/> (Null Object Pattern).
    /// Replace with a concrete implementation for actual persistence (database, file system, cloud storage)</description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// Most services are registered with <b>Scoped</b> lifetime, meaning a new instance is created
    /// per HTTP request. This ensures proper isolation and prevents state leaking between requests.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register RFC 7807 problem details for standardized error responses
        services.AddProblemDetails();

        // Register global exception handler for unhandled exceptions
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Register validation error response factory
        services.AddScoped<IValidationErrorResponseFactory, ValidationErrorResponseFactory>();

        // Register HMAC signature validator for crash report authentication
        services.AddScoped<ICrashReportRequestSignatureValidator, CrashReportRequestSignatureValidator>();

        // Register crash report sanitizer for defense-in-depth privacy protection
        services.AddScoped<ICrashReportSanitizer, CrashReportSanitizer>();

        // Register Null Object Pattern storage service (replace with real implementation for production)
        services.AddScoped<ICrashReportStorageService, NullCrashReportStorageService>();

        return services;
    }

    /// <summary>
    /// Configures health check services for liveness and readiness probes.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Health checks are essential for container orchestration platforms like Kubernetes
    /// to determine application health and readiness to serve traffic.
    /// 
    /// Default registration includes:
    /// <list type="bullet">
    /// <item><description>Basic health check service infrastructure</description></item>
    /// <item><description>No specific health checks registered (all checks pass by default)</description></item>
    /// </list>
    /// 
    /// Health check endpoints are mapped in <see cref="WebApplicationExtensions.MapEndpoints"/>:
    /// <list type="bullet">
    /// <item><description><c>/health/live</c> - Liveness probe (is the app running?)</description></item>
    /// <item><description><c>/health/ready</c> - Readiness probe (is the app ready for traffic?)</description></item>
    /// </list>
    /// 
    /// Future enhancements could include:
    /// <list type="bullet">
    /// <item><description>Database connectivity checks</description></item>
    /// <item><description>External service availability checks</description></item>
    /// <item><description>Disk space and memory checks</description></item>
    /// <item><description>Custom business logic health indicators</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services)
    {
        // Register health check services (basic infrastructure)
        HealthCheckServiceCollectionExtensions.AddHealthChecks(services);

        return services;
    }

    /// <summary>
    /// Configures request timeout policies to prevent long-running requests from consuming resources.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Request timeouts help protect the API from:
    /// <list type="bullet">
    /// <item><description>Slow or unresponsive clients</description></item>
    /// <item><description>Network connectivity issues</description></item>
    /// <item><description>Resource exhaustion from long-running operations</description></item>
    /// <item><description>Denial-of-service scenarios</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Default Policy:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Timeout: 10 seconds</description></item>
    /// <item><description>Status Code: HTTP 408 (Request Timeout)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Named Policies:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>CrashReportIngest</term>
    /// <description>10-second timeout for crash report submission endpoints.
    /// Ensures timely responses even for large crash reports with extensive logs.</description>
    /// </item>
    /// </list>
    /// 
    /// Apply named policies to controllers or actions using:
    /// <code>
    /// [RequestTimeout("CrashReportIngest")]
    /// public async Task&lt;IActionResult&gt; SubmitCrashReport(...)
    /// </code>
    /// </remarks>
    public static IServiceCollection ConfigureRequestTimeouts(this IServiceCollection services)
    {
        services.AddRequestTimeouts(options =>
        {
            // Default timeout policy for all requests
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(10),
                TimeoutStatusCode = StatusCodes.Status408RequestTimeout
            };

            // Named timeout policy for crash report ingestion
            options.AddPolicy("CrashReportIngest", new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(10),
                TimeoutStatusCode = StatusCodes.Status408RequestTimeout
            });
        });

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators for request and DTO validation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// FluentValidation provides a strongly-typed, fluent interface for defining validation rules
    /// on request DTOs. This approach offers several advantages over data annotations:
    /// <list type="bullet">
    /// <item><description>Separation of concerns - Validation logic is separated from model classes</description></item>
    /// <item><description>Reusability - Validators can be composed and shared across models</description></item>
    /// <item><description>Testability - Validators can be unit tested independently</description></item>
    /// <item><description>Complex rules - Supports conditional validation, custom validators, and async validation</description></item>
    /// <item><description>Clear error messages - Detailed, contextual validation error messages</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Registered Validators:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="SubmitCrashReportRequestValidator"/></term>
    /// <description>Validates crash report submission requests including HMAC signature and timestamp checks</description>
    /// </item>
    /// <item>
    /// <term><see cref="CrashReportDtoValidator"/></term>
    /// <description>Validates the main crash report DTO including application metadata and system information</description>
    /// </item>
    /// <item>
    /// <term><see cref="CrashExceptionInfoDtoValidator"/></term>
    /// <description>Validates exception information including type, message, and stack trace data</description>
    /// </item>
    /// <item>
    /// <term><see cref="CrashLogEntryDtoValidator"/></term>
    /// <description>Validates individual log entries included in crash reports</description>
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// All validators are registered with <b>Scoped</b> lifetime to align with the HTTP request lifecycle.
    /// Validation is automatically invoked by the <see cref="GlobalExceptionHandler"/> when validation
    /// errors are detected, and results are formatted by <see cref="ValidationErrorResponseFactory"/>
    /// into RFC 7807 problem details responses.
    /// </para>
    /// 
    /// Example validation error response:
    /// <code>
    /// {
    ///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ///   "title": "One or more validation errors occurred.",
    ///   "status": 400,
    ///   "errors": {
    ///     "CrashReport.AppVersion": ["AppVersion is required."],
    ///     "CrashReport.Exception.Type": ["Exception type cannot be empty."]
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<SubmitCrashReportRequest>, SubmitCrashReportRequestValidator>();
        services.AddScoped<IValidator<CrashReportDto>, CrashReportDtoValidator>();
        services.AddScoped<IValidator<CrashExceptionInfoDto>, CrashExceptionInfoDtoValidator>();
        services.AddScoped<IValidator<CrashLogEntryDto>, CrashLogEntryDtoValidator>();

        return services;
    }

    /// <summary>
    /// Configures JSON serialization options for the API using source-generated serialization context.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method configures JSON serialization for HTTP endpoints using System.Text.Json with
    /// Native AOT-compatible source generation for optimal performance and reduced startup time.
    /// 
    /// <para>
    /// <b>Source-Generated Serialization:</b>
    /// </para>
    /// The <see cref="CastaraJsonContext"/> is a source-generated JSON serialization context
    /// that provides the following benefits:
    /// <list type="bullet">
    /// <item><description>Native AOT compatibility - No runtime reflection required</description></item>
    /// <item><description>Faster startup - Serialization metadata generated at compile time</description></item>
    /// <item><description>Better performance - Optimized serialization code</description></item>
    /// <item><description>Smaller app size - No reflection-based serialization overhead</description></item>
    /// <item><description>Trim-safe - Works with IL trimming in published applications</description></item>
    /// </list>
    /// 
    /// <para>
    /// The serialization context is inserted at the beginning of the resolver chain to ensure
    /// it takes precedence over default reflection-based serialization.
    /// </para>
    /// 
    /// <para>
    /// <b>Serialization Context Registration:</b>
    /// </para>
    /// The context must include all types that will be serialized/deserialized by the API.
    /// Add new types to <see cref="CastaraJsonContext"/> when introducing new DTOs or response models.
    /// </remarks>
    public static IServiceCollection AddJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, CastaraJsonContext.Default);
        });

        return services;
    }
}