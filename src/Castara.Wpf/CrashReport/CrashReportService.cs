using Castara.Wpf.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Diagnostics.CrashReport.Upload.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Castara.Wpf.CrashReport;

/// <summary>
/// Production implementation of <see cref="ICrashReportService"/> that coordinates the complete
/// crash reporting workflow for WPF applications, including user interaction, local persistence,
/// and optional server submission.
/// </summary>
/// <remarks>
/// <para>
/// This service orchestrates the entire crash reporting process when fatal exceptions occur:
/// </para>
/// 
/// <para>
/// <b>Primary responsibilities:</b>
/// </para>
/// <list type="number">
/// <item><description><b>Build</b>: Construct comprehensive crash reports from exceptions using <see cref="ICrashReportBuilder"/></description></item>
/// <item><description><b>Display</b>: Show crash report dialog to user via <see cref="ICrashReportDialogService"/></description></item>
/// <item><description><b>Save</b>: Persist crash reports locally using <see cref="ICrashReportWriter"/> if user chooses</description></item>
/// <item><description><b>Upload</b>: Submit crash reports to diagnostic server using <see cref="ICrashReportUploader"/> if user chooses</description></item>
/// </list>
/// 
/// <para>
/// <b>Multi-layered error handling:</b>
/// </para>
/// This service implements defensive programming with three tiers of error handling:
/// <list type="bullet">
/// <item><description><b>Tier 1 - Primary workflow</b>: Build report → Show dialog → Save/Upload based on user choice</description></item>
/// <item><description><b>Tier 2 - Fallback</b>: If primary workflow fails, attempt to save crash report locally without dialog</description></item>
/// <item><description><b>Tier 3 - Last-ditch</b>: If fallback fails, log critical error (crash information may be lost)</description></item>
/// </list>
/// 
/// <para>
/// <b>Dependencies:</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="ICrashReportBuilder"/>: Constructs crash reports with sanitized paths and application state</description></item>
/// <item><description><see cref="ICrashReportWriter"/>: Writes crash reports to %LocalAppData%\Castara\CrashReports as JSON files</description></item>
/// <item><description><see cref="ICrashReportDialogService"/>: Shows WPF dialog allowing user to view, save, or upload crash report</description></item>
/// <item><description><see cref="ICrashReportUploader"/>: Submits crash reports to diagnostic server via HMAC-authenticated HTTP API</description></item>
/// <item><description><see cref="ILogger{TCategoryName}"/>: Records crash report workflow events for monitoring and troubleshooting</description></item>
/// </list>
/// 
/// <para>
/// <b>User interaction flow:</b>
/// </para>
/// <list type="number">
/// <item><description>Fatal exception occurs and is caught by global exception handler</description></item>
/// <item><description>Crash report is built with exception details, stack traces, app state, and recent logs</description></item>
/// <item><description>User is shown a dialog with the crash report JSON and action options</description></item>
/// <item><description>User chooses to: save locally, upload to server, both, or dismiss</description></item>
/// <item><description>Service performs requested actions (save and/or upload)</description></item>
/// <item><description>Upload success/failure is logged for monitoring</description></item>
/// <item><description>Application can then shutdown gracefully</description></item>
/// </list>
/// 
/// <para>
/// <b>Example usage in App.xaml.cs:</b>
/// </para>
/// <code>
/// protected override void OnStartup(StartupEventArgs e)
/// {
///     base.OnStartup(e);
///     
///     // Register global exception handler
///     DispatcherUnhandledException += async (s, args) =>
///     {
///         args.Handled = true; // Prevent default Windows error dialog
///         
///         // Handle crash reporting
///         await _crashReportService.HandleFatalAsync(
///             args.Exception, 
///             "DispatcherUnhandledException");
///         
///         // Shutdown application with error code
///         Application.Current.Shutdown(1);
///     };
/// }
/// </code>
/// 
/// <para>
/// <b>Thread safety:</b>
/// </para>
/// This service is not thread-safe by itself, but it's designed to be called on the UI thread
/// (Dispatcher thread) from global exception handlers. The dialog service requires UI thread execution.
/// </remarks>
public sealed class CrashReportService : ICrashReportService
{
    /// <summary>
    /// JSON serializer options configured for human-readable, indented output.
    /// </summary>
    /// <remarks>
    /// Indentation is essential for the crash report dialog, where users review the JSON
    /// before deciding whether to save or upload. Pretty-printed JSON is easier to read
    /// and copy-paste into bug reports or support tickets.
    /// </remarks>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ICrashReportBuilder _builder;
    private readonly ICrashReportWriter _writer;
    private readonly ICrashReportDialogService _dialogService;
    private readonly ICrashReportUploader _uploader;
    private readonly ILogger<CrashReportService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashReportService"/> class.
    /// </summary>
    /// <param name="builder">The crash report builder for constructing reports from exceptions.</param>
    /// <param name="writer">The crash report writer for persisting reports to local disk.</param>
    /// <param name="dialogService">The dialog service for displaying crash reports to users.</param>
    /// <param name="uploader">The uploader service for submitting crash reports to the diagnostic server.</param>
    /// <param name="logger">The logger for recording crash report workflow events.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public CrashReportService(
        ICrashReportBuilder builder,
        ICrashReportWriter writer,
        ICrashReportDialogService dialogService,
        ICrashReportUploader uploader,
        ILogger<CrashReportService> logger)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a fatal exception by building a crash report, showing it to the user via dialog,
    /// and performing user-requested actions (save locally and/or upload to server).
    /// </summary>
    /// <param name="exception">The fatal exception that occurred.</param>
    /// <param name="source">The exception source or handler context (e.g., "DispatcherUnhandledException").</param>
    /// <returns>A <see cref="Task"/> that completes when crash report processing finishes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is null, empty, or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="ICrashReportService.HandleFatalAsync"/> contract with
    /// a user-driven workflow that allows the user to choose whether to save and/or upload their
    /// crash report.
    /// </para>
    /// 
    /// <para>
    /// <b>Implementation notes:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Validates input parameters before processing (fail-fast)</description></item>
    /// <item><description>Builds crash report with sanitized paths and application state</description></item>
    /// <item><description>Serializes crash report to pretty-printed JSON for user review</description></item>
    /// <item><description>Shows modal dialog allowing user to view, save, upload, or dismiss</description></item>
    /// <item><description>Saves locally to %LocalAppData%\Castara\CrashReports if user chooses</description></item>
    /// <item><description>Uploads to server via HMAC-authenticated API if user chooses</description></item>
    /// <item><description>Logs all actions (save, upload success, upload failure) for monitoring</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Error handling strategy:</b>
    /// </para>
    /// <list type="number">
    /// <item><description><b>Primary workflow (try block)</b>: Build report → Show dialog → Save/Upload based on user choice</description></item>
    /// <item><description><b>Fallback (catch block)</b>: If primary workflow fails, attempt to save crash report locally without showing dialog</description></item>
    /// <item><description><b>Last-ditch (nested catch)</b>: If fallback fails, log critical error (crash information may be lost)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Possible outcomes:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>User saves locally</b>: Crash report saved to %LocalAppData%\Castara\CrashReports\{timestamp}_{reportId}.json</description></item>
    /// <item><description><b>User uploads</b>: Crash report submitted to server, success/failure logged</description></item>
    /// <item><description><b>User does both</b>: Crash report saved locally AND uploaded to server</description></item>
    /// <item><description><b>User dismisses</b>: No action taken (crash report discarded)</description></item>
    /// <item><description><b>Primary workflow fails</b>: Fallback save without dialog</description></item>
    /// <item><description><b>Fallback also fails</b>: Critical error logged, crash information may be lost</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Async behavior:</b>
    /// </para>
    /// This method is async to support asynchronous upload to the server via <see cref="ICrashReportUploader"/>.
    /// The file I/O and dialog display are synchronous operations on the UI thread. The calling code
    /// should await this method to ensure crash report processing completes before application shutdown.
    /// </remarks>
    public async Task HandleFatalAsync(Exception exception, string source)
    {
        // Validate input parameters upfront (fail-fast)
        ArgumentNullException.ThrowIfNull(exception);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Crash source is required.", nameof(source));

        try
        {
            // Build comprehensive crash report from exception
            var report = _builder.Build(exception, source);

            // Serialize crash report to pretty-printed JSON for user review
            var reportJson = JsonSerializer.Serialize(report, JsonOptions);

            // Show dialog to user and get their choice (save/upload/dismiss)
            var dialogResult = _dialogService.Show(reportJson, report.ReportId);

            string? savedPath = null;

            // Handle user's choice to save crash report locally
            if (dialogResult.SaveLocally)
            {
                savedPath = _writer.Write(report);
                _logger.LogInformation(
                    "Crash report {ReportId} saved locally to {Path}",
                    report.ReportId,
                    savedPath);
            }

            // Handle user's choice to upload crash report to diagnostic server
            if (dialogResult.SendReport)
            {
                var uploadResult = await _uploader
                    .UploadAsync(report, CancellationToken.None);

                if (uploadResult.Success)
                {
                    _logger.LogInformation(
                        "Crash report {ReportId} uploaded successfully. IncidentId={IncidentId}",
                        report.ReportId,
                        uploadResult.IncidentId);
                }
                else
                {
                    _logger.LogWarning(
                        "Crash report {ReportId} upload failed. Status={Status}, Error={Error}",
                        report.ReportId,
                        uploadResult.Status,
                        uploadResult.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            // Primary workflow failed - attempt fallback without dialog
            _logger.LogError(ex, "Failed while handling fatal crash report workflow.");

            // Last-ditch fallback: try to persist locally without showing dialog
            try
            {
                var fallbackReport = _builder.Build(exception, source);
                var fallbackPath = _writer.Write(fallbackReport);

                _logger.LogInformation(
                    "Fallback crash report {ReportId} saved locally to {Path}",
                    fallbackReport.ReportId,
                    fallbackPath);
            }
            catch (Exception fallbackEx)
            {
                // Even fallback failed - crash information may be lost
                _logger.LogCritical(
                    fallbackEx,
                    "Failed to save fallback crash report for fatal exception.");
            }
        }
    }
}
