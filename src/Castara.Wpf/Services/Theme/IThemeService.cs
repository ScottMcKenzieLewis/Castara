namespace Castara.Wpf.Services.Theme;

/// <summary>
/// Defines a service contract for managing application theme (dark/light mode) for
/// Material Design in XAML components.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a simplified abstraction for theme management throughout
/// the application. Implementations are responsible for coordinating theme changes
/// across all Material Design UI components.
/// </para>
/// <para>
/// <strong>Note:</strong> This service only affects Material Design components. OxyPlot
/// charts require separate theme management handled by view models that contain chart
/// visualizations.
/// </para>
/// <para>
/// The service should be registered as a singleton in the dependency injection container
/// to ensure consistent theming across the entire application.
/// </para>
/// </remarks>
/// <seealso cref="ThemeService"/>
public interface IThemeService
{
    /// <summary>
    /// Sets the application theme to dark or light mode.
    /// </summary>
    /// <param name="isDark">
    /// <c>true</c> to enable dark theme; <c>false</c> to enable light theme.
    /// </param>
    /// <remarks>
    /// <para>
    /// When invoked, this method should immediately update all Material Design components
    /// throughout the application, affecting:
    /// <list type="bullet">
    ///   <item><description>Background and surface colors</description></item>
    ///   <item><description>Text and icon colors</description></item>
    ///   <item><description>Elevation and shadow rendering</description></item>
    ///   <item><description>Border and divider colors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Theme changes persist for the application session but are not automatically
    /// saved. User preferences should be stored separately if persistence across
    /// sessions is required.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example usage in a view model:
    /// <code>
    /// public class ShellViewModel
    /// {
    ///     private readonly IThemeService _themeService;
    ///     
    ///     public ShellViewModel(IThemeService themeService)
    ///     {
    ///         _themeService = themeService;
    ///     }
    ///     
    ///     public void ToggleTheme(bool isDark)
    ///     {
    ///         _themeService.SetDark(isDark);
    ///         // Also update OxyPlot charts separately if needed
    ///     }
    /// }
    /// </code>
    /// </example>
    void SetDark(bool isDark);
}