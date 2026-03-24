using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Wpf.Models;

/// <summary>
/// Represents a casting profile option for display in the UI.
/// </summary>
public sealed class CastingProfileOption
{
    /// <summary>
    /// Gets or initializes the display name for the casting profile option.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the underlying casting profile definition.
    /// May be null for placeholder options.
    /// </summary>
    public CastingProfileDefinition? Profile { get; init; }

    /// <summary>
    /// Gets a value indicating whether this option is a placeholder without an actual profile.
    /// </summary>
    public bool IsPlaceholder => Profile is null;
}