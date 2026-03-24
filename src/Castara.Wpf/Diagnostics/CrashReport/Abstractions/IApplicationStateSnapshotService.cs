using Castara.Wpf.Diagnostics.CrashReport;

namespace Castara.Wpf.Diagnostics.CrashReport.Abstractions;

public interface IApplicationStateSnapshotService
{
    ApplicationStateSnapshot GetSnapshot();

    void SetTheme(string? theme);
    void SetActiveView(string? activeView);
    void SetSelectedCastingProfile(string? profileDisplayName);

    void SetField(string key, string? value);
    void RemoveField(string key);
    void ClearFields();
}
