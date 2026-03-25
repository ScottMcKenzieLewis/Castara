namespace Castara.Wpf.Diagnostics.CrashReport.Interfaces;

/// <summary>
/// Provides services for constructing comprehensive crash reports from exception data.
/// </summary>
/// <remarks>
/// <para>
/// This service is responsible for assembling all diagnostic information into a complete
/// crash report structure that can be serialized, saved, and analyzed for troubleshooting.
/// </para>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// The builder pattern is used here to separate the concerns of:
/// <list type="bullet">
///   <item><description>Data collection - Gathering exception details, system info, application state</description></item>
///   <item><description>Report assembly - Organizing collected data into a coherent report structure</description></item>
///   <item><description>Report persistence - Saving or transmitting the assembled report (handled by separate service)</description></item>
/// </list>
/// </para>
/// <para>
/// By isolating the building logic, we can:
/// <list type="bullet">
///   <item><description>Test crash report generation without triggering real exceptions</description></item>
///   <item><description>Mock report generation in integration tests</description></item>
///   <item><description>Extend the report structure without modifying persistence logic</description></item>
///   <item><description>Reuse the builder for different crash reporting scenarios</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Typical Data Collected:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Exception type, message, and full stack trace</description></item>
///   <item><description>Inner exception chain with recursive stack traces</description></item>
///   <item><description>Application state snapshot (current view, profile, settings)</description></item>
///   <item><description>System information (OS, .NET runtime, memory, CPU)</description></item>
///   <item><description>Timestamp and unique report identifier</description></item>
///   <item><description>Application version and build information</description></item>
/// </list>
/// </remarks>
public interface ICrashReportBuilder
{
    /// <summary>
    /// Constructs a complete crash report from an exception and source context.
    /// </summary>
    /// <param name="exception">
    /// The unhandled exception that triggered crash report generation.
    /// This can be any exception type, and the builder should handle inner exceptions
    /// and aggregate exceptions appropriately.
    /// </param>
    /// <param name="source">
    /// The source context where the exception was caught (e.g., "App.DispatcherUnhandledException",
    /// "TaskScheduler.UnobservedTaskException", "MainWindow.Initialize").
    /// This helps identify where in the application lifecycle the crash occurred.
    /// </param>
    /// <returns>
    /// A fully populated <see cref="CrashReport"/> containing exception details,
    /// application state snapshot, system information, and metadata ready for
    /// serialization and persistence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> or <paramref name="source"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// </para>
    /// <list type="number">
    ///   <item><description>Generate unique report ID (typically GUID or timestamp-based)</description></item>
    ///   <item><description>Extract exception information including full stack trace and inner exceptions</description></item>
    ///   <item><description>Capture current application state snapshot from <see cref="IApplicationStateSnapshotService"/></description></item>
    ///   <item><description>Collect system information (OS version, .NET runtime, memory, etc.)</description></item>
    ///   <item><description>Record timestamp and source context</description></item>
    ///   <item><description>Assemble all data into immutable <see cref="CrashReport"/> structure</description></item>
    /// </list>
    /// <para>
    /// The builder should be defensive and handle failures gracefully. If any part of data
    /// collection fails (e.g., unable to retrieve system info), the builder should include
    /// whatever information is available and note the collection failure rather than throwing
    /// an exception that could mask the original crash.
    /// </para>
    /// <para>
    /// <strong>Exception Chain Handling:</strong>
    /// </para>
    /// <para>
    /// For exceptions with inner exceptions, the builder should recursively collect information
    /// from the entire exception chain. For <see cref="AggregateException"/>, all inner exceptions
    /// should be flattened and included in the report.
    /// </para>
    /// </remarks>
    CrashReport Build(Exception exception, string source);
}
