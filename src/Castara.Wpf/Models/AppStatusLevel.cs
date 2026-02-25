namespace Castara.Wpf.Models;

/// <summary>
/// Defines the severity levels for application status messages displayed in the status bar.
/// </summary>
/// <remarks>
/// <para>
/// Status levels provide visual feedback about the current state of the application,
/// with each level associated with a specific color indicator to communicate urgency
/// and importance to the user.
/// </para>
/// <para>
/// These levels are used by <see cref="Services.Status.IStatusService"/> to communicate
/// operation results, validation issues, and error conditions throughout the application.
/// The <see cref="ViewModels.ShellViewModel"/> maps these levels to colored indicators
/// in the status bar.
/// </para>
/// <para>
/// Color associations:
/// <list type="table">
///   <listheader>
///     <term>Level</term>
///     <description>Color and Meaning</description>
///   </listheader>
///   <item>
///     <term><see cref="Ok"/></term>
///     <description>Green (#35C759) - Normal operation, successful completion</description>
///   </item>
///   <item>
///     <term><see cref="Warning"/></term>
///     <description>Yellow (#FFCC00) - Issues requiring attention but not critical</description>
///   </item>
///   <item>
///     <term><see cref="Error"/></term>
///     <description>Red (#FF3B30) - Critical errors or operation failures</description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public enum AppStatusLevel
{
    /// <summary>
    /// Indicates normal operation or successful completion of an operation.
    /// </summary>
    /// <remarks>
    /// This level is used when:
    /// <list type="bullet">
    ///   <item><description>The application is ready for user input</description></item>
    ///   <item><description>An operation completed successfully</description></item>
    ///   <item><description>Calculation results have no high-severity risk flags</description></item>
    /// </list>
    /// Displayed with a green indicator in the status bar.
    /// </remarks>
    Ok,

    /// <summary>
    /// Indicates a warning condition that requires user attention but does not prevent operation.
    /// </summary>
    /// <remarks>
    /// This level is used when:
    /// <list type="bullet">
    ///   <item><description>Input validation detects values outside recommended ranges</description></item>
    ///   <item><description>Calculation results include high-severity risk flags</description></item>
    ///   <item><description>Non-critical issues are detected that may affect results</description></item>
    /// </list>
    /// Displayed with a yellow indicator in the status bar.
    /// </remarks>
    Warning,

    /// <summary>
    /// Indicates a critical error or operation failure.
    /// </summary>
    /// <remarks>
    /// This level is used when:
    /// <list type="bullet">
    ///   <item><description>An operation fails due to an exception</description></item>
    ///   <item><description>Critical validation errors prevent processing</description></item>
    ///   <item><description>System errors occur (file access, service failures, etc.)</description></item>
    /// </list>
    /// Displayed with a red indicator in the status bar.
    /// </remarks>
    Error
}