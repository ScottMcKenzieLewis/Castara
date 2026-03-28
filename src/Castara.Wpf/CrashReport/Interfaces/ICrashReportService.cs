using Castara.Wpf.Diagnostics.CrashReport.Interfaces;

namespace Castara.Wpf.CrashReport.Interfaces;

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
    /// and notifying the user asynchronously.
    /// </summary>
    /// <param name="exception">
    /// The unhandled exception that caused the application to crash.
    /// Should never be null in normal circumstances, but implementations should
    /// handle null defensively by creating a synthetic exception.
    /// </param>
    /// <param name="source">
    /// The exception source or handler context (e.g., "DispatcherUnhandledException",
    /// "UnobservedTaskException", "AppDomainUnhandledException").
    /// This helps identify where in the application lifecycle the crash occurred.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that completes when crash report processing finishes. The task always
    /// completes successfully even if internal operations fail (graceful degradation).
    /// </returns>
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
    ///     file in the crash reports directory (typically %LocalAppData%\Castara\CrashReports)
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
    /// <strong>Async/Await Behavior:</strong>
    /// </para>
    /// <para>
    /// This method is async to support asynchronous file I/O operations, but implementations
    /// can complete synchronously using <c>Task.CompletedTask</c> if all operations are synchronous.
    /// Since this is called during fatal exception handling:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>The method should complete all work before returning (no fire-and-forget)</description></item>
    ///   <item><description>File I/O can use <c>async/await</c> for better performance</description></item>
    ///   <item><description>UI operations (dialogs) are typically synchronous on the UI thread</description></item>
    ///   <item><description>The calling code should <c>await</c> this method before shutdown</description></item>
    /// </list>
    /// <para>
    /// <strong>Example implementation pattern:</strong>
    /// </para>
    /// <code>
    /// public async Task HandleFatalAsync(Exception exception, string source)
    /// {
    ///     try
    ///     {
    ///         // Build crash report (synchronous)
    ///         var report = _builder.Build(exception, source);
    ///         
    ///         // Save to disk (asynchronous file I/O)
    ///         var path = await _writer.WriteAsync(report);
    ///         
    ///         // Show success dialog (synchronous UI operation)
    ///         _dialogService.ShowCrashReportSaved(path, report.ReportId);
    ///     }
    ///     catch
    ///     {
    ///         // Show failure dialog (synchronous UI operation)
    ///         _dialogService.ShowCrashReportFailed("Unable to save crash report.");
    ///     }
    /// }
    /// </code>
    /// <para>
    /// <strong>Alternative synchronous completion:</strong>
    /// </para>
    /// <code>
    /// public Task HandleFatalAsync(Exception exception, string source)
    /// {
    ///     try
    ///     {
    ///         var report = _builder.Build(exception, source);
    ///         var path = _writer.Write(report); // Synchronous write
    ///         _dialogService.ShowCrashReportSaved(path, report.ReportId);
    ///     }
    ///     catch
    ///     {
    ///         _dialogService.ShowCrashReportFailed("Unable to save crash report.");
    ///     }
    ///     
    ///     return Task.CompletedTask; // Synchronous completion
    /// }
    /// </code>
    /// <para>
    /// <strong>Application Termination:</strong>
    /// </para>
    /// <para>
    /// After this method completes, the application typically terminates (either through
    /// <c>Environment.Exit()</c> or natural shutdown). The calling code should await this
    /// method to ensure all crash report operations complete before termination:
    /// </para>
    /// <code>
    /// DispatcherUnhandledException += async (s, args) =>
    /// {
    ///     await _crashReportService.HandleFatalAsync(args.Exception, "DispatcherUnhandledException");
    ///     args.Handled = true;
    ///     Application.Current.Shutdown(1); // Exit with error code
    /// };
    /// </code>
    /// </remarks>
    Task HandleFatalAsync(Exception exception, string source);
}
