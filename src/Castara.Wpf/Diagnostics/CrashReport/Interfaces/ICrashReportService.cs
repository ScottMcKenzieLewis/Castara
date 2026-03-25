namespace Castara.Wpf.Diagnostics.CrashReport.Interfaces;

/// <summary>
/// Provides the primary crash reporting service for handling fatal application exceptions.
/// </summary>
/// <remarks>
/// <para>
/// This service acts as the main orchestrator for the crash reporting infrastructure,
/// coordinating the collection, persistence, and user notification of application crashes.
/// It should be called from global exception handlers to ensure comprehensive crash reporting.
/// </para>
/// <para>
/// <strong>Integration Points:</strong>
/// </para>
/// <para>
/// This service orchestrates several subordinate services:
/// <list type="bullet">
///   <item><description><see cref="ICrashReportBuilder"/> - Constructs crash reports from exception data</description></item>
///   <item><description><see cref="IApplicationStateSnapshotService"/> - Captures application state at crash time</description></item>
///   <item><description>File I/O - Persists crash reports to disk for analysis</description></item>
///   <item><description><see cref="ICrashDialogService"/> - Presents crash information to user</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Typical Usage:</strong>
/// </para>
/// <para>
/// Register handlers in application startup:
/// <code>
/// // In App.xaml.cs
/// protected override void OnStartup(StartupEventArgs e)
/// {
///     DispatcherUnhandledException += (s, args) =&gt;
///     {
///         _crashReportService.HandleFatal(args.Exception, "DispatcherUnhandledException");
///         args.Handled = true; // Prevent default crash dialog
///     };
///     
///     TaskScheduler.UnobservedTaskException += (s, args) =&gt;
///     {
///         _crashReportService.HandleFatal(args.Exception, "UnobservedTaskException");
///         args.SetObserved(); // Mark as handled
///     };
///     
///     AppDomain.CurrentDomain.UnhandledException += (s, args) =&gt;
///     {
///         _crashReportService.HandleFatal(
///             args.ExceptionObject as Exception ?? new Exception("Unknown fatal error"),
///             "AppDomainUnhandledException");
///     };
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Error Handling Philosophy:</strong>
/// </para>
/// <para>
/// Implementations must be maximally defensive. Since this service is called during fatal
/// exception handling, it should never throw exceptions that could mask or compound the
/// original crash. If crash report generation or persistence fails, the service should
/// gracefully degrade and present whatever diagnostic information is available to the user.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// </para>
/// <para>
/// Implementations should be thread-safe as fatal exceptions can occur on any thread
/// (UI thread, background worker, task pool thread, etc.).
/// </para>
/// </remarks>
public interface ICrashReportService
{
    /// <summary>
    /// Handles a fatal exception by generating a crash report, saving it to disk,
    /// and notifying the user.
    /// </summary>
    /// <param name="exception">
    /// The unhandled exception that caused the application to crash.
    /// Should never be null in normal circumstances, but implementations should
    /// handle null defensively by creating a synthetic exception.
    /// </param>
    /// <param name="source">
    /// The exception source or handler context (e.g., "App.DispatcherUnhandledException",
    /// "TaskScheduler.UnobservedTaskException", "MainWindow.OnLoaded").
    /// This helps identify where in the application lifecycle the crash occurred.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method performs the complete crash reporting workflow:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <strong>Build</strong> - Use <see cref="ICrashReportBuilder"/> to construct comprehensive
    ///     crash report from exception, application state snapshot, and system information
    ///   </description></item>
    ///   <item><description>
    ///     <strong>Persist</strong> - Serialize crash report to JSON and save to a timestamped
    ///     file in the crash reports directory (typically AppData/Castara/CrashReports)
    ///   </description></item>
    ///   <item><description>
    ///     <strong>Notify Success</strong> - If persistence succeeds, call <see cref="ICrashDialogService.ShowCrashReportSaved"/>
    ///     to inform user of crash report location and report ID
    ///   </description></item>
    ///   <item><description>
    ///     <strong>Notify Failure</strong> - If persistence fails, call <see cref="ICrashDialogService.ShowCrashReportFailed"/>
    ///     with fallback diagnostic text so user still receives some crash information
    ///   </description></item>
    /// </list>
    /// <para>
    /// <strong>Defensive Programming:</strong>
    /// </para>
    /// <para>
    /// Implementations must wrap all operations in try-catch blocks to prevent secondary
    /// exceptions during crash handling. If any step fails, the service should continue
    /// with degraded functionality rather than throwing. For example:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>If report building fails, attempt to save raw exception information</description></item>
    ///   <item><description>If file I/O fails, show error dialog with exception details</description></item>
    ///   <item><description>If dialog display fails, attempt fallback notification (console output, event log)</description></item>
    /// </list>
    /// <para>
    /// <strong>Non-Blocking:</strong>
    /// </para>
    /// <para>
    /// This method should complete synchronously and not use async/await, as it may be
    /// called during application shutdown where async continuations may not execute.
    /// </para>
    /// <para>
    /// <strong>Application Termination:</strong>
    /// </para>
    /// <para>
    /// After this method completes, the application typically terminates (either through
    /// <c>Environment.Exit()</c> or natural shutdown). The service should ensure all critical
    /// data is flushed to disk before returning.
    /// </para>
    /// </remarks>
    void HandleFatal(Exception exception, string source);
}
