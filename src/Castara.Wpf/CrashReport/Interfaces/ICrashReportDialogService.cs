using Castara.Wpf.Diagnostics.CrashReport;

namespace Castara.Wpf.CrashReport.Interfaces;

/// <summary>
/// Defines a contract for displaying crash report dialogs to users, allowing them to review
/// and take action on crash reports (view, copy, send to server, or discard).
/// </summary>
/// <remarks>
/// <para>
/// This service provides the user-facing UI for crash report handling in WPF applications.
/// When a crash occurs and a report is generated, this service presents the user with options
/// to review the crash report details, copy them to the clipboard, send them to a support server,
/// or discard them.
/// </para>
/// 
/// <para>
/// <b>User options typically include:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>View Details</b>: Display the full crash report JSON in a scrollable viewer</description></item>
/// <item><description><b>Copy to Clipboard</b>: Copy the crash report JSON for manual sharing or bug reporting</description></item>
/// <item><description><b>Send to Server</b>: Submit the crash report to a diagnostic server via HTTP API</description></item>
/// <item><description><b>Discard</b>: Close the dialog without taking action (future enhancement)</description></item>
/// </list>
/// 
/// <para>
/// <b>Implementation notes:</b>
/// </para>
/// This interface abstracts the WPF dialog implementation, enabling:
/// <list type="bullet">
/// <item><description>Testability: Mock implementations for unit tests</description></item>
/// <item><description>Flexibility: Different UI implementations (MaterialDesign, native WPF, custom themes)</description></item>
/// <item><description>Separation of concerns: Dialog logic separated from crash report generation</description></item>
/// </list>
/// 
/// <para>
/// <b>Common implementations:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>CrashReportDialogService</b>: Production implementation showing a WPF dialog</description></item>
/// <item><description><b>MockCrashReportDialogService</b>: Test implementation returning predefined results</description></item>
/// <item><description><b>AutoSubmitCrashReportDialogService</b>: Automated implementation for unattended scenarios</description></item>
/// </list>
/// 
/// <para>
/// <b>Example dialog workflow:</b>
/// </para>
/// <code>
/// // In global exception handler
/// var reportJson = JsonSerializer.Serialize(crashReport, options);
/// var result = _dialogService.Show(reportJson, crashReport.ReportId);
/// 
/// switch (result)
/// {
///     case CrashReportDialogResult.SendToServer:
///         await _submissionService.SubmitAsync(crashReport);
///         break;
///     case CrashReportDialogResult.CopyToClipboard:
///         // Already copied by dialog
///         break;
///     case CrashReportDialogResult.Discard:
///         // User closed dialog
///         break;
/// }
/// </code>
/// </remarks>
public interface ICrashReportDialogService
{
    /// <summary>
    /// Displays a modal dialog showing the crash report details and returns the user's chosen action.
    /// </summary>
    /// <param name="reportJson">The crash report in JSON format (human-readable, pretty-printed).</param>
    /// <param name="reportId">The unique report identifier displayed to the user for reference.</param>
    /// <returns>
    /// A <see cref="CrashReportDialogResult"/> indicating the action the user chose (send, copy, discard).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method displays a modal dialog that blocks the calling thread until the user makes a choice.
    /// The dialog should present the crash report in a user-friendly format and provide clear action buttons.
    /// </para>
    /// 
    /// <para>
    /// <b>User actions:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Send to Server</b>: User chooses to submit the crash report to the diagnostic server
    /// (returns <c>CrashReportDialogResult.SendToServer</c>)</description></item>
    /// <item><description><b>Copy to Clipboard</b>: User copies the crash report JSON to clipboard for manual sharing
    /// (returns <c>CrashReportDialogResult.CopyToClipboard</c>)</description></item>
    /// <item><description><b>Close/Discard</b>: User closes the dialog without action
    /// (returns <c>CrashReportDialogResult.Discard</c>)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Modal behavior:</b>
    /// </para>
    /// The dialog must be modal (blocking) to ensure the application waits for the user's decision
    /// before continuing shutdown or recovery procedures. This prevents race conditions where the
    /// application might terminate before the user can review or submit the crash report.
    /// 
    /// <para>
    /// <b>JSON formatting:</b>
    /// </para>
    /// The <paramref name="reportJson"/> should be pretty-printed (indented) JSON for readability.
    /// The dialog implementation should display it in a scrollable, read-only text viewer with
    /// syntax highlighting if possible (e.g., using AvalonEdit or similar).
    /// 
    /// <para>
    /// <b>Report ID display:</b>
    /// </para>
    /// The <paramref name="reportId"/> should be prominently displayed in the dialog so users can
    /// reference it when communicating with support. It may also be included in the window title.
    /// 
    /// <para>
    /// <b>Dialog features:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Scrollable JSON viewer with syntax highlighting</description></item>
    /// <item><description>Copy to clipboard button (copies <paramref name="reportJson"/>)</description></item>
    /// <item><description>Send to server button (initiates HTTP submission)</description></item>
    /// <item><description>Report ID display (for user reference)</description></item>
    /// <item><description>Timestamp display (when crash occurred)</description></item>
    /// <item><description>Clear action buttons (Material Design or native WPF)</description></item>
    /// </list>
    /// 
    /// <para>
    /// <b>Example usage:</b>
    /// </para>
    /// <code>
    /// public class GlobalExceptionHandler
    /// {
    ///     private readonly ICrashReportDialogService _dialogService;
    ///     private readonly ICrashReportSubmissionService _submissionService;
    ///     
    ///     public void HandleUnhandledException(Exception exception)
    ///     {
    ///         // Generate crash report
    ///         var report = _reportBuilder.Build(exception, "UnhandledException");
    ///         var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions 
    ///         { 
    ///             WriteIndented = true 
    ///         });
    ///         
    ///         // Show dialog to user
    ///         var result = _dialogService.Show(reportJson, report.ReportId);
    ///         
    ///         // Handle user's choice
    ///         switch (result)
    ///         {
    ///             case CrashReportDialogResult.SendToServer:
    ///                 // Submit crash report to server
    ///                 await _submissionService.SubmitAsync(report);
    ///                 MessageBox.Show("Crash report submitted. Thank you!");
    ///                 break;
    ///                 
    ///             case CrashReportDialogResult.CopyToClipboard:
    ///                 // Already copied by dialog
    ///                 MessageBox.Show("Crash report copied to clipboard.");
    ///                 break;
    ///                 
    ///             case CrashReportDialogResult.Discard:
    ///                 // User closed dialog without action
    ///                 break;
    ///         }
    ///         
    ///         // Continue with application shutdown or recovery
    ///     }
    /// }
    /// </code>
    /// 
    /// <para>
    /// <b>Thread safety:</b>
    /// </para>
    /// This method must be called on the UI thread (WPF Dispatcher thread). If called from a
    /// background thread, use <c>Dispatcher.Invoke</c> to marshal the call to the UI thread.
    /// </remarks>
    CrashReportDialogResult Show(string reportJson, string reportId);
}
