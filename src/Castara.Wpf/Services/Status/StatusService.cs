using System.ComponentModel;
using Castara.Wpf.Models;

namespace Castara.Wpf.Services.Status;

/// <summary>
/// Provides the default implementation of <see cref="IStatusService"/> for managing
/// application-wide status messages and level indicators.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains a single current status state and notifies subscribers
/// when the status changes through the <see cref="INotifyPropertyChanged"/> pattern.
/// All status updates are immediately reflected in bound UI components.
/// </para>
/// <para>
/// The service is initialized with a default "Ready" status indicating the application
/// is prepared for user interaction. Status updates are typically made by:
/// <list type="bullet">
///   <item><description>View models after completing operations (calculations, validation)</description></item>
///   <item><description>Commands in response to user actions</description></item>
///   <item><description>Exception handlers to display error information</description></item>
/// </list>
/// </para>
/// <para>
/// This implementation is thread-safe for reading the current status but should only
/// be modified from the UI thread to ensure proper property change notifications.
/// </para>
/// </remarks>
public sealed class StatusService : IStatusService
{
    /// <summary>
    /// The current status state backing field, initialized with default "Ready" status.
    /// </summary>
    private StatusState _current = new(AppStatusLevel.Ok, "Ready", "Ready for Calcuation");

    /// <summary>
    /// Gets the current status state of the application.
    /// </summary>
    /// <value>
    /// A <see cref="StatusState"/> containing the current status level and display texts.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property raises <see cref="PropertyChanged"/> when the status changes,
    /// enabling automatic UI updates through WPF data binding.
    /// </para>
    /// <para>
    /// The property uses value comparison to avoid unnecessary change notifications
    /// when the same status is set multiple times.
    /// </para>
    /// </remarks>
    public StatusState Current
    {
        get => _current;
        private set
        {
            if (Equals(_current, value)) return;
            _current = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        }
    }

    /// <summary>
    /// Occurs when the <see cref="Current"/> property value changes.
    /// </summary>
    /// <remarks>
    /// This event enables WPF data binding to automatically update UI elements
    /// when the status changes. Subscribers can monitor status changes to implement
    /// custom behaviors or logging.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the application status using a pre-constructed status state.
    /// </summary>
    /// <param name="state">
    /// The <see cref="StatusState"/> containing the level and text information to display.
    /// </param>
    /// <remarks>
    /// This method updates the <see cref="Current"/> property, triggering property
    /// change notification if the new state differs from the current state.
    /// </remarks>
    public void Set(StatusState state) => Current = state;

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
    /// This is the most commonly used method for updating status, providing a
    /// convenient way to set all status components in a single call.
    /// </para>
    /// <para>
    /// The method creates a new <see cref="StatusState"/> instance and assigns it
    /// to <see cref="Current"/>, triggering property change notification if the
    /// new state differs from the current state.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example usage patterns:
    /// <code>
    /// // Success with no issues
    /// statusService.Set(AppStatusLevel.Ok, "Calculated", "No risks");
    /// 
    /// // Warning with details
    /// statusService.Set(AppStatusLevel.Warning, "Check inputs", "Carbon out of range");
    /// 
    /// // Error condition
    /// statusService.Set(AppStatusLevel.Error, "Calculation failed", ex.Message);
    /// </code>
    /// </example>
    public void Set(AppStatusLevel level, string leftText, string rightText)
        => Current = new StatusState(level, leftText, rightText);
}