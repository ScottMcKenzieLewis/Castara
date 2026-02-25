namespace Castara.Wpf.Infrastructure.Abstractions;

/// <summary>
/// Defines a contract for components that need to respond to application theme changes,
/// typically for updating visualizations that don't automatically bind to WPF theme resources.
/// </summary>
/// <remarks>
/// <para>
/// This interface is primarily used by view models that contain OxyPlot charts or other
/// custom visualizations that require explicit theme updates. While WPF controls and
/// Material Design components automatically respond to theme changes through resource
/// binding, some third-party controls need manual theme synchronization.
/// </para>
/// <para>
/// <strong>Implementation Pattern:</strong>
/// </para>
/// <para>
/// View models implementing this interface should rebuild or refresh their visualization
/// models when <see cref="SetTheme"/> is called, applying appropriate colors and styling
/// for the specified theme mode.
/// </para>
/// <para>
/// <strong>Coordination:</strong>
/// </para>
/// <para>
/// The <see cref="ViewModels.ShellViewModel"/> typically coordinates theme changes by:
/// <list type="number">
///   <item><description>Calling <see cref="Services.Theme.IThemeService.SetDark"/> for Material Design components</description></item>
///   <item><description>Calling <see cref="SetTheme"/> on all theme-aware view models</description></item>
/// </list>
/// This ensures consistent theming across both Material Design UI and custom visualizations.
/// </para>
/// </remarks>
/// <example>
/// Example implementation in a view model with OxyPlot charts:
/// <code>
/// public class CalculationsViewModel : IThemeAware
/// {
///     private bool _isDarkTheme;
///     
///     public void SetTheme(bool isDark)
///     {
///         _isDarkTheme = isDark;
///         
///         // Rebuild plot models with appropriate theme colors
///         RebuildPlotsForTheme();
///         
///         // Refresh visualizations with current data
///         UpdateCharts();
///     }
///     
///     private void RebuildPlotsForTheme()
///     {
///         // Create new plot models with theme-appropriate colors
///         CompositionPlotModel = BuildCompositionModel(_isDarkTheme);
///         // ... rebuild other charts
///     }
/// }
/// </code>
/// </example>
public interface IThemeAware
{
    /// <summary>
    /// Notifies the component that the application theme has changed, allowing it to
    /// update visualizations and styling accordingly.
    /// </summary>
    /// <param name="isDark">
    /// <c>true</c> if the application is switching to dark theme; 
    /// <c>false</c> if switching to light theme.
    /// </param>
    /// <remarks>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    ///   <item><description>Update visualization models (charts, graphs) with theme-appropriate colors</description></item>
    ///   <item><description>Refresh any cached rendering data</description></item>
    ///   <item><description>Trigger UI updates through property change notifications</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is typically called by the shell or application-level view model
    /// when the user toggles the theme setting. The implementation should complete
    /// quickly to avoid UI lag during theme transitions.
    /// </para>
    /// </remarks>
    void SetTheme(bool isDark);
}