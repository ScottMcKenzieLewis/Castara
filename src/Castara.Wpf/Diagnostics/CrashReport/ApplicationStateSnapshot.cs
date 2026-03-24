namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed record ApplicationStateSnapshot(
    string? Theme,
    string? ActiveView,
    string? SelectedCastingProfile,
    IReadOnlyDictionary<string, string> Fields);
