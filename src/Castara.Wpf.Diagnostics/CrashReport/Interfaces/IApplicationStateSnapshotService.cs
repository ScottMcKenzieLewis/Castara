using Castara.Wpf.Diagnostics.CrashReport;

namespace Castara.Wpf.Diagnostics.CrashReport.Interfaces;

/// <summary>
/// Provides services for capturing and managing application state snapshots used in crash reporting.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains a dynamic collection of key-value pairs representing the current
/// application state. When an unhandled exception occurs, the crash reporting system can
/// retrieve a snapshot of this state to include in diagnostic reports.
/// </para>
/// <para>
/// <strong>Typical Usage:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Set state values as user navigates through the application (e.g., current view, selected profile)</description></item>
///   <item><description>Update state when important actions occur (e.g., calculations performed, data loaded)</description></item>
///   <item><description>Retrieve snapshot when crash occurs to include in crash report</description></item>
///   <item><description>Clear state when appropriate (e.g., user logout, session reset)</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong> Implementations should be thread-safe as state may be
/// updated from UI thread and read from exception handling context.
/// </para>
/// </remarks>
public interface IApplicationStateSnapshotService
{
    /// <summary>
    /// Retrieves a snapshot of the current application state.
    /// </summary>
    /// <returns>
    /// An <see cref="ApplicationStateSnapshot"/> containing a point-in-time copy of all
    /// currently tracked state values.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns a snapshot (immutable copy) rather than a live reference,
    /// ensuring that the returned state cannot be modified and represents the exact
    /// state at the moment the snapshot was taken.
    /// </para>
    /// <para>
    /// Called by crash reporting infrastructure when capturing diagnostic information
    /// during exception handling.
    /// </para>
    /// </remarks>
    ApplicationStateSnapshot GetSnapshot();

    /// <summary>
    /// Sets or updates a state value identified by the specified key.
    /// </summary>
    /// <param name="key">
    /// The unique identifier for the state value. Use constants from
    /// <see cref="ApplicationStateKeys"/> for standard keys.
    /// </param>
    /// <param name="value">
    /// The state value to store. If <c>null</c>, the key is typically removed
    /// or stored with a null representation depending on implementation.
    /// </param>
    /// <remarks>
    /// <para>
    /// If the key already exists, its value is updated. If the key does not exist,
    /// a new key-value pair is added to the state collection.
    /// </para>
    /// <para>
    /// <strong>Common Use Cases:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Track current view or navigation state</description></item>
    ///   <item><description>Record selected casting profile</description></item>
    ///   <item><description>Store active unit system (Standard/American)</description></item>
    ///   <item><description>Capture theme state (Light/Dark mode)</description></item>
    ///   <item><description>Log recent user actions or calculations</description></item>
    /// </list>
    /// </remarks>
    void SetValue(string key, string? value);

    /// <summary>
    /// Removes a state value identified by the specified key.
    /// </summary>
    /// <param name="key">
    /// The unique identifier of the state value to remove.
    /// </param>
    /// <remarks>
    /// <para>
    /// If the key does not exist in the state collection, this method has no effect
    /// and does not throw an exception.
    /// </para>
    /// <para>
    /// Use this method to clean up state values that are no longer relevant or
    /// to clear sensitive information before crash reporting.
    /// </para>
    /// </remarks>
    void RemoveValue(string key);

    /// <summary>
    /// Removes all state values from the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method clears the entire state collection, removing all key-value pairs.
    /// Subsequent calls to <see cref="GetSnapshot"/> will return an empty snapshot
    /// until new values are added via <see cref="SetValue"/>.
    /// </para>
    /// <para>
    /// <strong>Common Use Cases:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Reset application state when starting a new session</description></item>
    ///   <item><description>Clear state during testing or diagnostic scenarios</description></item>
    ///   <item><description>Remove all tracked data when user explicitly logs out</description></item>
    /// </list>
    /// </remarks>
    void Clear();
}
