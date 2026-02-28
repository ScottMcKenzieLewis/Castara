using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace Castara.Wpf.Telemetry.Logging;

/// <summary>
/// Thread-safe, WPF-friendly log store that marshals changes to the UI thread.
/// </summary>
/// <remarks>
/// This implementation ensures all collection modifications occur on the UI thread,
/// making it safe to bind directly to WPF controls. Entries are automatically trimmed
/// when the capacity is exceeded, removing the oldest entries first (FIFO).
/// </remarks>
public sealed class ObservableLogStore : IObservableLogStore
{
    private readonly object _gate = new();
    private readonly ObservableCollection<LogEntry> _entries = new();
    private readonly ReadOnlyObservableCollection<LogEntry> _readonly;
    private readonly Dispatcher _dispatcher;
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableLogStore"/> class.
    /// </summary>
    /// <param name="dispatcher">The WPF dispatcher used to marshal collection changes to the UI thread.</param>
    /// <param name="capacity">The maximum number of log entries to retain. Minimum value is 100.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcher"/> is null.</exception>
    public ObservableLogStore(Dispatcher dispatcher, int capacity = 2_000)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        if (capacity < 100) capacity = 100;
        _capacity = capacity;

        _readonly = new ReadOnlyObservableCollection<LogEntry>(_entries);
    }

    /// <summary>
    /// Gets a read-only observable collection of log entries for UI binding.
    /// </summary>
    /// <value>
    /// A read-only observable collection that automatically notifies UI controls of changes.
    /// </value>
    public ReadOnlyObservableCollection<LogEntry> Entries => _readonly;

    /// <summary>
    /// Adds a log entry to the store, marshaling to the UI thread if necessary.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    /// <remarks>
    /// If the capacity is exceeded, the oldest entry will be removed automatically.
    /// </remarks>
    public void Add(LogEntry entry)
    {
        // Ensure collection changes happen on UI thread.
        if (_dispatcher.CheckAccess())
        {
            AddOnUiThread(entry);
            return;
        }

        _dispatcher.BeginInvoke(() => AddOnUiThread(entry), DispatcherPriority.Background);
    }

    /// <summary>
    /// Adds a log entry on the UI thread with capacity enforcement.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    private void AddOnUiThread(LogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);

            // Trim oldest entries if capacity exceeded
            while (_entries.Count > _capacity)
                _entries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Clears all log entries from the store, marshaling to the UI thread if necessary.
    /// </summary>
    public void Clear()
    {
        if (_dispatcher.CheckAccess())
        {
            ClearOnUiThread();
            return;
        }

        _dispatcher.BeginInvoke(() => ClearOnUiThread(), DispatcherPriority.Background);
    }

    /// <summary>
    /// Clears all log entries on the UI thread.
    /// </summary>
    private void ClearOnUiThread()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Retrieves a thread-safe snapshot of all current log entries.
    /// </summary>
    /// <returns>A read-only list containing copies of all current log entries.</returns>
    /// <remarks>
    /// This method creates a snapshot that is safe to use outside the UI thread,
    /// such as for exporting logs or diagnostics. Changes to the store after
    /// this call will not affect the returned list.
    /// </remarks>
    public IReadOnlyList<LogEntry> GetSnapshot()
    {
        lock (_gate)
        {
            return _entries.ToList();
        }
    }
}