using Castara.Wpf.Models;

namespace Castara.Wpf.Infrastructure.Abstractions;

/// <summary>
/// Defines a contract for components that support multiple unit systems.
/// </summary>
/// <remarks>
/// Implement this interface for view models or services that need to adapt
/// their behavior, display, or calculations based on the active unit system
/// (e.g., metric vs imperial units).
/// </remarks>
public interface IUnitAware
{
    /// <summary>
    /// Gets or sets the unit system used by this component.
    /// </summary>
    /// <value>
    /// The active unit system that determines how measurements are displayed and interpreted.
    /// </value>
    UnitSystem UnitSystem { get; set; }
}