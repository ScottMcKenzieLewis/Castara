using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Castara.Domain.Estimation.Services;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf;

/// <summary>
/// The main application class for Castara, responsible for dependency injection configuration,
/// service registration, logging infrastructure, and application lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// This application uses the Generic Host pattern (<see cref="IHost"/>) from Microsoft.Extensions.Hosting
/// to provide:
/// <list type="bullet">
///   <item><description>Dependency injection with service lifetime management</description></item>
///   <item><description>Structured logging with multiple providers (Debug, Custom LogStore)</description></item>
///   <item><description>Configuration management and options pattern support</description></item>
///   <item><description>Graceful startup and shutdown with async support</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Exception Handling:</strong> Global exception handlers are configured for three sources:
/// <list type="number">
///   <item><description>UI thread exceptions (DispatcherUnhandledException)</description></item>
///   <item><description>Task exceptions (TaskScheduler.UnobservedTaskException)</description></item>
///   <item><description>AppDomain exceptions (UnhandledException) as last resort</description></item>
/// </list>
/// All exceptions are logged at Critical level for diagnostics and troubleshooting.
/// </para>
/// <para>
/// <strong>WPF Binding Errors:</strong> Data binding errors are captured via PresentationTraceSources
/// and logged through the application's logging infrastructure for visibility during development
/// and debugging.
/// </para>
/// </remarks>
public partial class App : Application
{
    /// <summary>
    /// The generic host that provides dependency injection, logging, and lifecycle management.
    /// </summary>
    private IHost? _host;

    /// <summary>
    /// Handles the application startup event, configuring the host, services, and logging infrastructure.
    /// </summary>
    /// <param name="e">The startup event arguments containing command-line arguments.</param>
    /// <remarks>
    /// <para>
    /// The startup process follows these steps:
    /// <list type="number">
    ///   <item><description>Create and configure the Generic Host with DI services</description></item>
    ///   <item><description>Configure logging providers (Debug, custom LogStore)</description></item>
    ///   <item><description>Set minimum log level to Information</description></item>
    ///   <item><description>Register global exception handlers for all exception sources</description></item>
    ///   <item><description>Hook WPF binding errors to the logging system</description></item>
    ///   <item><description>Start the host (activates hosted services if any)</description></item>
    ///   <item><description>Resolve MainWindow and ShellViewModel through DI</description></item>
    ///   <item><description>Display the main window</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach ensures all dependencies are properly initialized and logging is active
    /// before any application components are created, providing comprehensive diagnostics
    /// from the moment the application starts.
    /// </para>
    /// </remarks>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build host with dependency injection and logging
        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, Dispatcher);
            })
            .ConfigureLogging((context, logging) =>
            {
                // Clear default providers to avoid duplicate logs
                logging.ClearProviders();

                // Add Visual Studio Output window logging
                logging.AddDebug();

                // Register custom log store provider for in-app log viewer
                // (registered via DI so it can receive ILogStore dependency)
                logging.Services.AddSingleton<ILoggerProvider, LogStoreLoggingProvider>();

                // Set minimum level (Trace/Debug will be filtered unless explicitly enabled)
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Setup global exception handling and binding error capture
        HookGlobalExceptionHandlers(_host);
        HookWpfBindingErrorsToLogs(_host.Services);

        // Start the host (activates any hosted services)
        _host.Start();

        // Create and show main window through dependency injection
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<ShellViewModel>();
        mainWindow.Show();
    }

    /// <summary>
    /// Handles the application exit event, performing graceful shutdown of the host.
    /// </summary>
    /// <param name="e">The exit event arguments containing the exit code.</param>
    /// <remarks>
    /// <para>
    /// This method performs graceful shutdown with a 2-second timeout:
    /// <list type="number">
    ///   <item><description>Stops the host asynchronously (allows hosted services to clean up)</description></item>
    ///   <item><description>Disposes the host to release all resources</description></item>
    ///   <item><description>Clears the host reference</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The async void pattern is acceptable here as this is the final exit point.
    /// Exceptions during shutdown are caught by the host infrastructure and logged.
    /// </para>
    /// </remarks>
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(2));
            }
            finally
            {
                _host.Dispose();
                _host = null;
            }
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Configures all application services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="uiDispatcher">The WPF UI thread dispatcher for thread marshalling.</param>
    /// <remarks>
    /// <para>
    /// Services are registered with appropriate lifetimes:
    /// </para>
    /// <para>
    /// <strong>Infrastructure Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="Dispatcher"/> (Singleton) - UI thread marshalling for thread-safe operations</description></item>
    ///   <item><description><see cref="IObservableLogStore"/> (Singleton) - Thread-safe log storage with UI binding support</description></item>
    ///   <item><description><see cref="ILogStore"/> (Singleton) - Simplified interface for log providers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Domain Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="ICastIronEstimator"/> (Singleton) - Core estimation engine for cast iron properties</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Application Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="IStatusService"/> (Singleton) - Application-wide status and notification management</description></item>
    ///   <item><description><see cref="IThemeService"/> (Singleton) - Material Design theme switching and customization</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>View Models:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="CalculationsViewModel"/> (Singleton) - Calculations view model with theme awareness</description></item>
    ///   <item><description><see cref="ShellViewModel"/> (Singleton) - Shell navigation, theme coordination, and log viewing</description></item>
    ///   <item><description><see cref="IThemeAware"/> (Singleton collection) - All theme-aware components for coordinated updates</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Views:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="MainWindow"/> (Transient) - New instance per resolution</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="IObservableLogStore"/> is registered with a capacity of 5000 entries,
    /// automatically trimming oldest entries when exceeded to prevent memory issues.
    /// </para>
    /// </remarks>
    private static void ConfigureServices(IServiceCollection services, Dispatcher uiDispatcher)
    {
        // --------------------
        // WPF UI Thread Dispatcher
        // --------------------
        services.AddSingleton(uiDispatcher);

        // --------------------
        // Telemetry / Log Store (UI-bindable)
        // --------------------
        // Single instance safe to bind to in UI (uses ObservableCollection)
        services.AddSingleton<IObservableLogStore>(sp =>
            new ObservableLogStore(
                dispatcher: sp.GetRequiredService<Dispatcher>(),
                capacity: 5000));

        // Expose as ILogStore too (simpler interface for providers)
        services.AddSingleton<ILogStore>(sp => sp.GetRequiredService<IObservableLogStore>());

        // --------------------
        // Domain Services
        // --------------------
        services.AddSingleton<ICastIronEstimator, CastIronEstimator>();

        // --------------------
        // Application Services
        // --------------------
        services.AddSingleton<IStatusService, StatusService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // --------------------
        // View Models
        // --------------------
        // Singleton ensures one long-lived instance with state preservation
        services.AddSingleton<CalculationsViewModel>();
        services.AddSingleton<ShellViewModel>();

        // Register all theme-aware components for coordinated theme updates
        services.AddSingleton<IThemeAware>(sp => sp.GetRequiredService<CalculationsViewModel>());

        // Register all unit-aware components for unit related calculations
        services.AddSingleton<IUnitAware>(sp => sp.GetRequiredService<CalculationsViewModel>());

        // --------------------
        // Windows / Views
        // --------------------
        services.AddTransient<MainWindow>();
    }

    /// <summary>
    /// Configures global exception handlers for UI, Task, and AppDomain unhandled exceptions.
    /// </summary>
    /// <param name="host">The application host containing the logging infrastructure.</param>
    /// <remarks>
    /// <para>
    /// This method registers handlers for three exception sources:
    /// </para>
    /// <para>
    /// <strong>1. UI Thread Exceptions (DispatcherUnhandledException):</strong>
    /// Catches exceptions on the WPF UI thread. The exception is logged at Critical level
    /// and marked as handled to prevent application termination. This allows graceful
    /// recovery from UI-related errors.
    /// </para>
    /// <para>
    /// <strong>2. Task Exceptions (TaskScheduler.UnobservedTaskException):</strong>
    /// Catches unobserved exceptions from async operations and background tasks. The exception
    /// is logged at Critical level and marked as observed to prevent finalizer thread crashes.
    /// </para>
    /// <para>
    /// <strong>3. AppDomain Exceptions (UnhandledException):</strong>
    /// Last-resort handler for any unhandled exceptions not caught by the above handlers.
    /// Logs at Critical level with termination status. These exceptions typically terminate
    /// the application.
    /// </para>
    /// <para>
    /// All handlers use structured logging with <see cref="ILogger{T}"/> to ensure exceptions
    /// are captured in the log store for diagnostics and troubleshooting.
    /// </para>
    /// </remarks>
    private static void HookGlobalExceptionHandlers(IHost host)
    {
        var log = host.Services.GetRequiredService<ILogger<App>>();

        // 1) UI thread exceptions
        Current.DispatcherUnhandledException += (s, e) =>
        {
            log.LogCritical(e.Exception, "Unhandled UI (Dispatcher) exception");
            e.Handled = true; // Prevent application termination

            // Optional: Show user-friendly error dialog
            // MessageBox.Show(e.Exception.Message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // 2) Non-UI thread exceptions (TaskScheduler)
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            log.LogCritical(e.Exception, "Unobserved Task exception");
            e.SetObserved(); // Prevent finalizer thread crash
        };

        // 3) AppDomain exceptions (last resort)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                log.LogCritical(
                    ex,
                    "Unhandled AppDomain exception (IsTerminating={IsTerminating})",
                    e.IsTerminating);
            }
            else
            {
                log.LogCritical(
                    "Unhandled AppDomain exception (non-Exception): {ExceptionObject}",
                    e.ExceptionObject);
            }
        };
    }

    /// <summary>
    /// Configures WPF data binding error capture and routing to the logging infrastructure.
    /// </summary>
    /// <param name="services">The service provider containing the logging infrastructure.</param>
    /// <remarks>
    /// <para>
    /// This method enables capture of WPF binding errors that would otherwise only appear
    /// in the Output window. By routing these errors through the logging system:
    /// <list type="bullet">
    ///   <item><description>Binding errors appear in the in-app log viewer for easier debugging</description></item>
    ///   <item><description>Errors are structured and timestamped consistently with other logs</description></item>
    ///   <item><description>Binding problems are visible in production builds without attached debugger</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The trace source is configured to capture only Error-level events to avoid
    /// performance overhead from verbose binding diagnostics.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This is primarily useful during development and testing.
    /// Binding errors should be fixed rather than ignored in production.
    /// </para>
    /// </remarks>
    private static void HookWpfBindingErrorsToLogs(IServiceProvider services)
    {
        var log = services.GetRequiredService<ILogger<App>>();

        // Configure trace source to capture only errors
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

        // Add custom listener that forwards to logging infrastructure
        PresentationTraceSources.DataBindingSource.Listeners.Add(
            new LoggerTraceListener(msg => log.LogError("WPF Binding: {Message}", msg)));
    }

    /// <summary>
    /// A custom trace listener that forwards trace messages to the application's logging infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This listener bridges the gap between WPF's legacy <see cref="TraceListener"/> system
    /// and the modern <see cref="ILogger"/> infrastructure, ensuring all diagnostic information
    /// flows through a unified logging pipeline.
    /// </para>
    /// <para>
    /// The listener trims whitespace from messages to ensure clean log entries and ignores
    /// empty messages to avoid log noise.
    /// </para>
    /// </remarks>
    private sealed class LoggerTraceListener : TraceListener
    {
        private readonly Action<string> _write;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerTraceListener"/> class.
        /// </summary>
        /// <param name="write">
        /// An action to invoke for each trace message, typically writing to <see cref="ILogger"/>.
        /// </param>
        public LoggerTraceListener(Action<string> write)
            => _write = write;

        /// <summary>
        /// Writes a trace message to the logging infrastructure.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <remarks>
        /// Empty or whitespace-only messages are ignored to prevent log pollution.
        /// Messages are trimmed to remove leading/trailing whitespace.
        /// </remarks>
        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _write(message.Trim());
        }

        /// <summary>
        /// Writes a trace message followed by a line terminator to the logging infrastructure.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <remarks>
        /// Delegates to <see cref="Write(string?)"/> as line terminators are not needed
        /// in structured logging systems.
        /// </remarks>
        public override void WriteLine(string? message) => Write(message);
    }
}