namespace Castara.Wpf.Diagnostics.CrashReport;

public sealed record CrashContextSnapshot(
    string? Theme,
    string? ActiveView,
    string? SelectedCastingProfile,
    IReadOnlyDictionary<string, string> Fields);