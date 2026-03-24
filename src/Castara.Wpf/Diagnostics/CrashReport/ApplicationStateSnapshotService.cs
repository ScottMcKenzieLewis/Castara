using Castara.Wpf.Diagnostics.CrashReport.Abstractions;
using System.Collections.Concurrent;

namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed class ApplicationStateSnapshotService : IApplicationStateSnapshotService
{
    private readonly object _gate = new();

    private string? _theme;
    private string? _activeView;
    private string? _selectedCastingProfile;

    private readonly ConcurrentDictionary<string, string> _fields = new();

    public ApplicationStateSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new ApplicationStateSnapshot(
                Theme: _theme,
                ActiveView: _activeView,
                SelectedCastingProfile: _selectedCastingProfile,
                Fields: new Dictionary<string, string>(_fields));
        }
    }

    public void SetTheme(string? theme)
    {
        lock (_gate)
        {
            _theme = Normalize(theme);
        }
    }

    public void SetActiveView(string? activeView)
    {
        lock (_gate)
        {
            _activeView = Normalize(activeView);
        }
    }

    public void SetSelectedCastingProfile(string? profileDisplayName)
    {
        lock (_gate)
        {
            _selectedCastingProfile = Normalize(profileDisplayName);
        }
    }

    public void SetField(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var normalizedValue = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            _fields.TryRemove(key, out _);
            return;
        }

        _fields[key] = normalizedValue;
    }

    public void RemoveField(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _fields.TryRemove(key, out _);
    }

    public void ClearFields()
    {
        _fields.Clear();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
