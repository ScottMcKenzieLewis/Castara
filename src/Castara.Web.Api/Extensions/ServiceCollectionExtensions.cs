using Asp.Versioning;
using Castara.Api.Configuration;
using Castara.Api.Diagnostics.Services;
using Castara.Api.Dtos;
using Castara.Api.Dtos.Responses;
using Castara.Api.Exceptions;
using Castara.Api.Services.Diagnostics;
using Castara.Diagnostics.Api.Services.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Services.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Castara.Api.Extensions;

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
        // Register ASP.NET Core MVC controllers
        services.AddControllers();

        // Register organized service groups
        services.AddApiVersioning();
        services.AddRateLimiting(configuration);
        services.ConfigureRequestTimeouts();
        services.AddOpenApi();
        services.AddApplicationServices();
        services.AddHealthChecks();
        services.AddValidators();

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
        .AddMvc();

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
    /// Configures OpenAPI (Swagger) documentation generation and AutoMapper for DTO mappings.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers services for:
    /// <list type="bullet">
    /// <item><description><b>API Explorer</b>: Generates API metadata for Swagger</description></item>
    /// <item><description><b>Swagger Generator</b>: Creates OpenAPI specification documents</description></item>
    /// <item><description><b>AutoMapper</b>: Maps between domain objects and DTOs</description></item>
    /// </list>
    /// 
    /// AutoMapper scans the assembly containing <see cref="Program"/> for:
    /// <list type="bullet">
    /// <item><description>Classes inheriting from <c>Profile</c></description></item>
    /// <item><description>Mapping configurations between domain and DTO types</description></item>
    /// </list>
    /// 
    /// Examples of mapped types:
    /// <list type="bullet">
    /// <item><description>DateOnly ↔ LocalDate (NodaTime)</description></item>
    /// <item><description>ValuationResult&lt;Bond&gt; ↔ BondValuationResponseDto</description></item>
    /// <item><description>ValuationLine ↔ ValuationLineDto</description></item>
    /// </list>
    /// 
    /// Swagger UI is available at:
    /// <list type="bullet">
    /// <item><description><c>/swagger</c> - Interactive API documentation</description></item>
    /// <item><description><c>/swagger/v1/swagger.json</c> - OpenAPI specification</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        // Register API Explorer for metadata generation
        services.AddEndpointsApiExplorer();
        
        // Register Swagger documentation generator
        services.AddSwaggerGen();
        
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register RFC 7807 problem details for standardized error responses
        services.AddProblemDetails();
        
        // Register global exception handler for unhandled exceptions
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddScoped<IValidationErrorResponseFactory, ValidationErrorResponseFactory>();

        services.AddScoped<ICrashReportRequestSignatureValidator, CrashReportRequestSignatureValidator>();

        services.AddScoped<ICrashReportSanitizer, CrashReportSanitizer>();

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

    public static IServiceCollection ConfigureRequestTimeouts(this IServiceCollection services)
    {
        // Register health check services (basic infrastructure)
        services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(10),
                TimeoutStatusCode = StatusCodes.Status408RequestTimeout
            };

            options.AddPolicy("CrashReportIngest", new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(10),
                TimeoutStatusCode = StatusCodes.Status408RequestTimeout
            });
        });

        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<SubmitCrashReportRequest>, SubmitCrashReportRequestValidator>();
        services.AddScoped<IValidator<CrashReportDto>, CrashReportDtoValidator>();
        services.AddScoped<IValidator<CrashExceptionInfoDto>, CrashExceptionInfoDtoValidator>();
        services.AddScoped<IValidator<CrashLogEntryDto>, CrashLogEntryDtoValidator>();

        return services;
    }

}