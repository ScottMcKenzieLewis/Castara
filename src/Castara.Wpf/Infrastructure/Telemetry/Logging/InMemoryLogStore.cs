using System;
using System.Collections.Generic;
using System.Linq;

namespace Castara.Wpf.Infrastructure.Telemetry.Logging;

/// <summary>
/// Thread-safe, in-memory log store with capacity-based automatic trimming.
/// </summary>
/// <remarks>
/// This implementation uses a simple <see cref="List{T}"/> for storage and does not
/// support WPF data binding. For UI binding scenarios, use <see cref="ObservableLogStore"/> instead.
/// When capacity is exceeded, the oldest entries are removed first (FIFO).
/// </remarks>
public sealed class InMemoryLogStore : ILogStore
{
    private readonly object _gate = new();
    private readonly List<LogEntry> _entries = new();
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryLogStore"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of log entries to retain. Minimum value is 100.</param>
    public InMemoryLogStore(int capacity = 2_000)
    {
        if (capacity < 100) capacity = 100;
        _capacity = capacity;
    }

    /// <summary>
    /// Adds a log entry to the store in a thread-safe manner.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    /// <remarks>
    /// If the capacity is exceeded after adding the entry, the oldest entries
    /// will be removed automatically to maintain the configured capacity.
    /// </remarks>
    public void Add(LogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);

            // Trim oldest entries if capacity exceeded
            if (_entries.Count > _capacity)
            {
                int toRemove = _entries.Count - _capacity;
                _entries.RemoveRange(0, toRemove);
            }
        }
    }

    /// <summary>
    /// Removes all log entries from the store in a thread-safe manner.
    /// </summary>
    public void Clear()
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
    /// This method creates a snapshot that is safe to use across threads.
    /// Changes to the store after this call will not affect the returned list.
    /// </remarks>
    public IReadOnlyList<LogEntry> GetSnapshot()
    {
        lock (_gate)
        {
            return _entries.ToList();
        }
    }
}