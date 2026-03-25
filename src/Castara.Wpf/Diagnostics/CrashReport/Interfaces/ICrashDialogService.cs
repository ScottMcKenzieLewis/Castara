namespace Castara.Wpf.Diagnostics.CrashReport.Interfaces;

/// <summary>
/// Provides user interface dialogs for communicating crash report generation results to the user.
/// </summary>
/// <remarks>
/// <para>
/// This service abstracts the presentation of crash reporting outcomes, allowing the crash
/// reporting infrastructure to remain UI-agnostic while still providing appropriate user feedback.
/// </para>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// By separating dialog presentation from crash report generation logic, we achieve:
/// <list type="bullet">
///   <item><description>Testability - Crash reporting logic can be tested without UI dependencies</description></item>
///   <item><description>Flexibility - Dialog implementation can be changed (MessageBox, custom window, toast notification) without affecting core logic</description></item>
///   <item><description>Separation of Concerns - UI presentation concerns are isolated from diagnostic data collection</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Typical Workflow:</strong>
/// </para>
/// <list type="number">
///   <item><description>Unhandled exception occurs in application</description></item>
///   <item><description>Crash reporting service attempts to generate and save crash report</description></item>
///   <item><description>If successful, <see cref="ShowCrashReportSaved"/> is called to inform user of saved report location</description></item>
///   <item><description>If failed, <see cref="ShowCrashReportFailed"/> is called to display error information</description></item>
/// </list>
/// </remarks>
public interface ICrashDialogService
{
    /// <summary>
    /// Displays a dialog informing the user that a crash report was successfully generated and saved.
    /// </summary>
    /// <param name="filePath">
    /// The full file system path where the crash report was saved.
    /// This allows users to locate and share the report file for support purposes.
    /// </param>
    /// <param name="reportId">
    /// A unique identifier for the crash report (typically a GUID or timestamp-based identifier).
    /// This can be used to reference the specific crash when communicating with support.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method is called when crash report generation and file I/O operations complete successfully.
    /// The dialog should present the information in a way that helps users take appropriate next steps,
    /// such as contacting support or submitting the report.
    /// </para>
    /// <para>
    /// <strong>Recommended Dialog Content:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Clear notification that the application encountered an error</description></item>
    ///   <item><description>Location of the saved crash report file for easy access</description></item>
    ///   <item><description>Report ID for reference in support communications</description></item>
    ///   <item><description>Instructions for next steps (e.g., "Contact support with Report ID")</description></item>
    ///   <item><description>Option to open file location or copy path to clipboard</description></item>
    /// </list>
    /// <para>
    /// The dialog should be modal to ensure user acknowledgment before the application terminates.
    /// </para>
    /// </remarks>
    void ShowCrashReportSaved(string filePath, string reportId);

    /// <summary>
    /// Displays a dialog informing the user that crash report generation failed.
    /// </summary>
    /// <param name="fallbackMessage">
    /// A fallback message containing essential crash information that could not be saved to a file.
    /// This typically includes exception type, message, and basic stack trace information
    /// to provide at least minimal diagnostic data when file I/O fails.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method is called when the crash reporting system itself encounters an error,
    /// such as file I/O failures, permission issues, or disk space problems. Despite the
    /// failure to save a complete crash report, the application should still attempt to
    /// present whatever diagnostic information is available to the user.
    /// </para>
    /// <para>
    /// <strong>Recommended Dialog Content:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Notification that crash report could not be saved</description></item>
    ///   <item><description>Display of fallback message with basic exception information</description></item>
    ///   <item><description>Option to copy error information to clipboard manually</description></item>
    ///   <item><description>Suggestion to contact support and manually provide error details</description></item>
    ///   <item><description>Apology for the inconvenience and assurance that issue is being investigated</description></item>
    /// </list>
    /// <para>
    /// The dialog should be designed to be as robust as possible since it represents a
    /// last-resort attempt to communicate crash information when the primary crash reporting
    /// mechanism has already failed.
    /// </para>
    /// </remarks>
    void ShowCrashReportFailed(string fallbackMessage);
}