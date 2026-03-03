using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf.Infrastructure.Telemetry.Logging;

/// <summary>
/// Custom <see cref="ILogger"/> implementation that writes log entries to an <see cref="ILogStore"/>.
/// </summary>
/// <remarks>
/// This logger captures structured properties and scope information from Microsoft.Extensions.Logging
/// and stores them in a format suitable for display in the WPF application. It integrates with
/// the standard .NET logging infrastructure while providing access to logs for diagnostic UI.
/// </remarks>
internal sealed class LogStoreLogger : ILogger
{
    private readonly string _category;
    private readonly ILogStore _store;
    private readonly Func<IExternalScopeProvider?> _getScopes;
    private static int _nextEventId;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogStoreLogger"/> class.
    /// </summary>
    /// <param name="category">The category name for the logger, typically the type name.</param>
    /// <param name="store">The log store where entries will be written.</param>
    /// <param name="getScopes">A function to retrieve the current external scope provider.</param>
    public LogStoreLogger(string category, ILogStore store, Func<IExternalScopeProvider?> getScopes)
    {
        _category = category;
        _store = store;
        _getScopes = getScopes;
    }

    /// <summary>
    /// Begins a logical operation scope. This implementation returns a no-op scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>A disposable object that ends the logical operation scope on dispose.</returns>
    /// <remarks>
    /// Scopes are captured via <see cref="IExternalScopeProvider"/> instead, so this returns a no-op.
    /// </remarks>
    IDisposable ILogger.BeginScope<TState>(TState state)
        => NullScope.Instance;

    /// <summary>
    /// Checks if the given <paramref name="logLevel"/> is enabled.
    /// </summary>
    /// <param name="logLevel">The level to be checked.</param>
    /// <returns><c>true</c> if enabled; otherwise, <c>false</c>.</returns>
    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None;

    /// <summary>
    /// Writes a log entry to the store.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="eventId">The event identifier associated with the log.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry, if any.</param>
    /// <param name="formatter">Function to create a message string from the state and exception.</param>
    /// <remarks>
    /// This method extracts structured properties and active logging scopes, then creates a
    /// <see cref="LogEntry"/> that is added to the configured <see cref="ILogStore"/>.
    /// </remarks>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter is null)
            return;

        var message = formatter(state, exception);

        // Capture structured properties (the "state" is often IEnumerable<KVP> in MEL)
        var props = ExtractProperties(state);

        // Capture scopes (very useful for correlation / operations)
        var scopes = ExtractScopes();

        var eid = eventId.Id == 0
            ? new EventId(
                System.Threading.Interlocked.Increment(ref _nextEventId),
                eventId.Name)
            : eventId;

        var entry = new LogEntry(
            Timestamp: DateTimeOffset.Now,
            Level: logLevel,
            Category: _category,
            EventId: eid,
            Message: message,
            Exception: exception,
            Properties: props,
            Scopes: scopes);

        _store.Add(entry);
    }

    /// <summary>
    /// Extracts structured properties from the log state.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="state">The state object, potentially containing structured key-value pairs.</param>
    /// <returns>A read-only list of key-value pairs representing the log properties.</returns>
    /// <remarks>
    /// If the state implements <see cref="IEnumerable{T}"/> of key-value pairs (as with structured logging),
    /// those pairs are extracted. Otherwise, the state is wrapped as a single "State" property.
    /// </remarks>
    private static IReadOnlyList<KeyValuePair<string, object?>> ExtractProperties<TState>(TState state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
        {
            // Keep all properties including {OriginalFormat} for diagnostics
            return kvps.ToList();
        }

        // If state is not structured, store it as a single property
        return new[] { new KeyValuePair<string, object?>("State", state) };
    }

    /// <summary>
    /// Extracts all active logging scopes from the external scope provider.
    /// </summary>
    /// <returns>A read-only list of key-value pairs representing all active scopes.</returns>
    /// <remarks>
    /// Scopes are useful for correlation and grouping related log entries. If scopes are structured
    /// (implement <see cref="IEnumerable{T}"/> of key-value pairs), their properties are extracted;
    /// otherwise, the scope object is stored under a "Scope" key.
    /// </remarks>
    private IReadOnlyList<KeyValuePair<string, object?>> ExtractScopes()
    {
        var provider = _getScopes();
        if (provider is null) return Array.Empty<KeyValuePair<string, object?>>();

        var list = new List<KeyValuePair<string, object?>>();

        provider.ForEachScope((scopeObj, state) =>
        {
            // If scope itself is structured, keep it structured
            if (scopeObj is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                foreach (var kvp in kvps)
                    state.Add(kvp);
            }
            else
            {
                state.Add(new KeyValuePair<string, object?>("Scope", scopeObj));
            }
        }, list);

        return list;
    }

    /// <summary>
    /// A no-op disposable scope implementation.
    /// </summary>
    /// <remarks>
    /// This is used as a singleton for <see cref="ILogger.BeginScope{TState}"/> since
    /// scopes are handled externally via <see cref="IExternalScopeProvider"/>.
    /// </remarks>
    private sealed class NullScope : IDisposable
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="NullScope"/>.
        /// </summary>
        public static readonly NullScope Instance = new();

        /// <summary>
        /// Performs no operation when disposed.
        /// </summary>
        public void Dispose() { }
    }
}