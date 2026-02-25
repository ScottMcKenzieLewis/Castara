using System.ComponentModel;
using Castara.Wpf.Models;

namespace Castara.Wpf.Services.Status;

/// <summary>
/// Defines a service contract for managing application-wide status messages and level indicators.
/// </summary>
/// <remarks>
/// <para>
/// This service provides centralized status management for the application, allowing
/// components to display contextual information, operation results, and warnings in
/// the status bar. The service implements <see cref="INotifyPropertyChanged"/> to
/// support reactive WPF data binding.
/// </para>
/// <para>
/// Status messages consist of:
/// <list type="bullet">
///   <item><description><strong>Level:</strong> Severity indicator (Ok, Warning, Error)</description></item>
///   <item><description><strong>Left Text:</strong> Primary status message (e.g., "Ready", "Calculated", "Error occurred")</description></item>
///   <item><description><strong>Right Text:</strong> Contextual information (e.g., "No risks", "2 risk(s)", connection status)</description></item>
/// </list>
/// </para>
/// <para>
/// The service is typically registered as a singleton in the dependency injection
/// container to ensure all components share the same status state.
/// </para>
/// </remarks>
/// <example>
/// Example usage in a view model:
/// <code>
/// // Set success status
/// statusService.Set(AppStatusLevel.Ok, "Calculated", "No risks");
/// 
/// // Set warning status
/// statusService.Set(AppStatusLevel.Warning, "Check inputs", "Carbon out of range");
/// 
/// // Set error status
/// statusService.Set(AppStatusLevel.Error, "Calculation failed", "Invalid section parameters");
/// </code>
/// </example>
public interface IStatusService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the current status state of the application.
    /// </summary>
    /// <value>
    /// A <see cref="StatusState"/> containing the current status level and display texts.
    /// </value>
    /// <remarks>
    /// This property is reactive and raises <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// when the status changes, allowing the UI to automatically update via data binding.
    /// </remarks>
    StatusState Current { get; }

    /// <summary>
    /// Sets the application status using a pre-constructed status state.
    /// </summary>
    /// <param name="state">
    /// The <see cref="StatusState"/> containing the level and text information to display.
    /// </param>
    /// <remarks>
    /// This overload is useful when a <see cref="StatusState"/> instance has already been
    /// created, avoiding the need to deconstruct and reconstruct the state.
    /// </remarks>
    void Set(StatusState state);

    /// <summary>
    /// Sets the application status by specifying individual components.
    /// </summary>
    /// <param name="level">
    /// The severity level of the status (Ok, Warning, or Error).
    /// </param>
    /// <param name="leftText">
    /// The primary status message displayed on the left side of the status bar
    /// (e.g., "Ready", "Calculating...", "Error occurred").
    /// </param>
    /// <param name="rightText">
    /// The contextual information displayed on the right side of the status bar
    /// (e.g., "No risks", "2 risk(s)", "SQLite • Local").
    /// </param>
    /// <remarks>
    /// <para>
    /// This is the most commonly used overload, providing a convenient way to set
    /// status without constructing a <see cref="StatusState"/> instance explicitly.
    /// </para>
    /// <para>
    /// The <paramref name="level"/> affects the visual appearance of status indicators
    /// (e.g., green for Ok, yellow for Warning, red for Error).
    /// </para>
    /// </remarks>
    void Set(AppStatusLevel level, string leftText, string rightText);
}