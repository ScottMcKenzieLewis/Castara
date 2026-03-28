using Castara.Wpf.Diagnostics.CrashReport;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using System.Collections.Concurrent;

/// <summary>
/// Provides thread-safe management of application state key-value pairs for crash reporting.
/// </summary>
public sealed class ApplicationStateSnapshotService : IApplicationStateSnapshotService
{
    private readonly ConcurrentDictionary<string, string> _values =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Creates an immutable snapshot of the current application state.
    /// </summary>
    /// <returns>An <see cref="ApplicationStateSnapshot"/> containing a copy of all current state values.</returns>
    public ApplicationStateSnapshot GetSnapshot()
    {
        var copy = new Dictionary<string, string>(_values, StringComparer.Ordinal);
        return new ApplicationStateSnapshot(copy);
    }

    /// <summary>
    /// Sets or updates a state value associated with the specified key.
    /// If the value is <see langword="null"/> or whitespace, the key is removed.
    /// </summary>
    /// <param name="key">The key to set. Whitespace is trimmed.</param>
    /// <param name="value">The value to associate with the key, or <see langword="null"/> to remove the key.</param>
    public void SetValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var normalizedKey = key.Trim();
        var normalizedValue = Normalize(value);

        if (normalizedValue is null)
        {
            _values.TryRemove(normalizedKey, out _);
            return;
        }

        _values[normalizedKey] = normalizedValue;
    }

    /// <summary>
    /// Removes the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to remove. Whitespace is trimmed.</param>
    public void RemoveValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _values.TryRemove(key.Trim(), out _);
    }

    /// <summary>
    /// Removes all state values.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    /// <summary>
    /// Normalizes a value by trimming whitespace, returning <see langword="null"/> for empty or whitespace-only strings.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The trimmed value, or <see langword="null"/> if the input is <see langword="null"/>, empty, or whitespace.</returns>
    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}