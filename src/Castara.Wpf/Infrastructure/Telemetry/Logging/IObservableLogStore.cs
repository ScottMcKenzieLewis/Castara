using System.Collections.ObjectModel;

namespace Castara.Wpf.Infrastructure.Telemetry.Logging;

/// <summary>
/// Extends <see cref="ILogStore"/> with an observable collection suitable for WPF data binding.
/// </summary>
/// <remarks>
/// This interface is designed for WPF scenarios where log entries need to be displayed
/// in UI controls (e.g., DataGrid, ListBox). The observable collection automatically
/// notifies bound controls when entries are added or removed.
/// </remarks>
public interface IObservableLogStore : ILogStore
{
    /// <summary>
    /// Gets a read-only observable collection of log entries for data binding to WPF controls.
    /// </summary>
    /// <value>
    /// A read-only observable collection that automatically notifies subscribers of changes.
    /// </value>
    ReadOnlyObservableCollection<LogEntry> Entries { get; }
}