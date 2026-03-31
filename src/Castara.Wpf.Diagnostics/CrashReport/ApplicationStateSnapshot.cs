namespace Castara.Wpf.Diagnostics.CrashReport;

/// <summary>
/// Represents an immutable snapshot of the application state at a specific point in time.
/// </summary>
/// <param name="Values">The dictionary containing application state key-value pairs.</param>
public sealed record ApplicationStateSnapshot(
    IReadOnlyDictionary<string, string> Values)
{
    /// <summary>
    /// Gets the value associated with the specified key, or <see langword="null"/> if the key is not found.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The value associated with the key, or <see langword="null"/> if the key does not exist.</returns>
    public string? Get(string key)
        => Values.TryGetValue(key, out var value) ? value : null;
}
