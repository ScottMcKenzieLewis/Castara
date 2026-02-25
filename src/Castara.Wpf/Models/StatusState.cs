namespace Castara.Wpf.Models;

/// <summary>
/// Represents an immutable snapshot of the application's status bar state, including
/// severity level and display text for both left and right sections.
/// </summary>
/// <param name="Level">
/// The severity level of the status, determining the visual indicator color and urgency.
/// </param>
/// <param name="LeftText">
/// The primary status message displayed on the left side of the status bar.
/// Typically describes the current operation state (e.g., "Ready", "Calculated", "Error occurred").
/// </param>
/// <param name="RightText">
/// The contextual information displayed on the right side of the status bar.
/// Typically provides additional details or context (e.g., "No risks", "2 risk(s)", "SQLite • Local").
/// </param>
/// <remarks>
/// <para>
/// This immutable record is used by <see cref="Services.Status.IStatusService"/> to maintain
/// and communicate application status throughout the UI. The record's value-based equality
/// ensures that duplicate status updates don't trigger unnecessary property change notifications.
/// </para>
/// <para>
/// The status state is displayed in the application's status bar with:
/// <list type="bullet">
///   <item><description><strong>Visual Indicator:</strong> A colored circle showing the status level</description></item>
///   <item><description><strong>Left Text:</strong> Primary message indicating current operation or state</description></item>
///   <item><description><strong>Right Text:</strong> Secondary information or context</description></item>
/// </list>
/// </para>
/// <para>
/// The <see cref="Level"/> property determines the color of the status indicator:
/// <list type="bullet">
///   <item><description><see cref="AppStatusLevel.Ok"/> → Green indicator</description></item>
///   <item><description><see cref="AppStatusLevel.Warning"/> → Yellow indicator</description></item>
///   <item><description><see cref="AppStatusLevel.Error"/> → Red indicator</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example status states for different scenarios:
/// <code>
/// // Application ready state
/// var ready = new StatusState(AppStatusLevel.Ok, "Ready", "SQLite • Local");
/// 
/// // Successful calculation with no risks
/// var success = new StatusState(AppStatusLevel.Ok, "Calculated", "No risks");
/// 
/// // Warning with validation issue
/// var warning = new StatusState(AppStatusLevel.Warning, "Check inputs", "Carbon out of range");
/// 
/// // Error condition
/// var error = new StatusState(AppStatusLevel.Error, "Calculation failed", "Invalid parameters");
/// </code>
/// </example>
public sealed record StatusState(
    AppStatusLevel Level,
    string LeftText,
    string RightText);