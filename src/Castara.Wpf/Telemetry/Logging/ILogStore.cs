using System;
using System.Collections.Generic;

namespace Castara.Wpf.Telemetry.Logging;

/// <summary>
/// Defines a store for managing application log entries.
/// </summary>
/// <remarks>
/// This interface provides a simple in-memory log storage mechanism
/// for diagnostic and debugging purposes in the WPF application.
/// For UI binding scenarios, prefer concrete implementations like ObservableLogStore.
/// </remarks>
public interface ILogStore
{
    /// <summary>
    /// Adds a log entry to the store.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    void Add(LogEntry entry);

    /// <summary>
    /// Removes all log entries from the store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Retrieves a read-only snapshot of all log entries in the store.
    /// </summary>
    /// <returns>A read-only collection of log entries.</returns>
    /// <remarks>
    /// This method is intended for non-UI consumers such as file exports or diagnostics.
    /// For UI binding, prefer concrete implementations like ObservableLogStore that support data binding.
    /// </remarks>
    IReadOnlyList<LogEntry> GetSnapshot();
}