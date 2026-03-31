namespace Castara.Wpf.CrashReport;

/// <summary>
/// Represents the immutable result of displaying a crash report dialog to the user,
/// indicating their choices regarding crash report handling.
/// </summary>
/// <param name="Accepted">
/// <see langword="true"/> if the user acknowledged the crash dialog and made an active choice;
/// <see langword="false"/> if the user dismissed or closed the dialog without taking action.
/// </param>
/// <param name="SendReport">
/// <see langword="true"/> if the user chose to send the crash report to the diagnostic server;
/// <see langword="false"/> otherwise.
/// </param>
/// <param name="SaveLocally">
/// <see langword="true"/> if the user chose to save the crash report to local disk;
/// <see langword="false"/> otherwise.
/// </param>
/// <remarks>
/// <para>
/// This record encapsulates the user's decision when presented with a crash report dialog.
/// The three boolean properties allow for flexible combinations of actions:
/// </para>
/// 
/// <para>
/// <b>Common result combinations:</b>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Accepted</term>
/// <term>SendReport</term>
/// <term>SaveLocally</term>
/// <term>Meaning</term>
/// </listheader>
/// <item>
/// <term>true</term>
/// <term>true</term>
/// <term>false</term>
/// <term>User chose to send crash report to server only</term>
/// </item>
/// <item>
/// <term>true</term>
/// <term>false</term>
/// <term>true</term>
/// <term>User chose to save crash report locally only</term>
/// </item>
/// <item>
/// <term>true</term>
/// <term>true</term>
/// <term>true</term>
/// <term>User chose both: send to server AND save locally</term>
/// </item>
/// <item>
/// <term>true</term>
/// <term>false</term>
/// <term>false</term>
/// <term>User viewed the report but chose not to save or send (copy to clipboard only)</term>
/// </item>
/// <item>
/// <term>false</term>
/// <term>false</term>
/// <term>false</term>
/// <term>User dismissed the dialog without taking action (closed/canceled)</term>
/// </item>
/// </list>
/// 
/// <para>
/// <b>Usage example:</b>
/// </para>
/// <code>
/// var result = _dialogService.Show(reportJson, reportId);
/// 
/// if (!result.Accepted)
/// {
///     _logger.LogInformation("User dismissed crash report dialog");
///     return;
/// }
/// 
/// if (result.SendReport)
/// {
///     await _submissionService.SubmitAsync(crashReport);
///     _logger.LogInformation("Crash report submitted to server");
/// }
/// 
/// if (result.SaveLocally)
/// {
///     var path = await _fileService.SaveAsync(crashReport);
///     _logger.LogInformation("Crash report saved to {Path}", path);
/// }
/// 
/// if (!result.SendReport &amp;&amp; !result.SaveLocally)
/// {
///     _logger.LogInformation("User viewed report but chose not to save or send");
/// }
/// </code>
/// 
/// <para>
/// <b>Immutability:</b>
/// </para>
/// As a <see langword="record"/>, this type is immutable. Once created, the values cannot be changed.
/// This ensures that dialog results cannot be accidentally modified after being returned.
/// 
/// <para>
/// <b>Factory pattern suggestion:</b>
/// </para>
/// Consider using factory methods for common scenarios to improve code readability:
/// <code>
/// public static class CrashReportDialogResult
/// {
///     public static CrashReportDialogResult Dismissed() 
///         => new(Accepted: false, SendReport: false, SaveLocally: false);
///     
///     public static CrashReportDialogResult SendOnly() 
///         => new(Accepted: true, SendReport: true, SaveLocally: false);
///     
///     public static CrashReportDialogResult SaveOnly() 
///         => new(Accepted: true, SendReport: false, SaveLocally: true);
///     
///     public static CrashReportDialogResult SendAndSave() 
///         => new(Accepted: true, SendReport: true, SaveLocally: true);
///     
///     public static CrashReportDialogResult ViewedOnly() 
///         => new(Accepted: true, SendReport: false, SaveLocally: false);
/// }
/// 
/// // Usage with factory methods:
/// return CrashReportDialogResult.SendOnly();
/// </code>
/// 
/// <para>
/// <b>Design rationale:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Three booleans</b>: Provides maximum flexibility for different user choices</description></item>
/// <item><description><b>Explicit flags</b>: Clear intent (no magic enums or string constants)</description></item>
/// <item><description><b>Immutable record</b>: Thread-safe, hashable, comparable by value</description></item>
/// <item><description><b>Separate concerns</b>: Send vs save are independent choices</description></item>
/// </list>
/// 
/// This type is returned by <see cref="Interfaces.ICrashReportDialogService.Show"/> to communicate
/// the user's decision back to the crash reporting orchestration logic.
/// </remarks>
public sealed record CrashReportDialogResult(
    bool Accepted,
    bool SendReport,
    bool SaveLocally);
