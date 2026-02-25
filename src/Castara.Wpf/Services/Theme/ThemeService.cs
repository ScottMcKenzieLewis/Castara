using MaterialDesignThemes.Wpf;

namespace Castara.Wpf.Services.Theme;

/// <summary>
/// Provides theme management services for Material Design in XAML components,
/// enabling runtime switching between dark and light themes.
/// </summary>
/// <remarks>
/// <para>
/// This service wraps the Material Design <see cref="PaletteHelper"/> to provide
/// a simplified interface for theme switching throughout the application. When the
/// theme changes, all Material Design components (buttons, cards, text fields, etc.)
/// automatically update their appearance.
/// </para>
/// <para>
/// <strong>Note:</strong> This service only affects Material Design UI components.
/// OxyPlot charts require separate theme management, which is handled by
/// <see cref="ViewModels.CalculationsViewModel.SetTheme(bool)"/>.
/// </para>
/// <para>
/// The service is registered as a singleton in the application's dependency injection
/// container to ensure consistent theming across all views and components.
/// </para>
/// </remarks>
public sealed class ThemeService : IThemeService
{
    /// <summary>
    /// The Material Design palette helper used to manipulate theme settings.
    /// </summary>
    private readonly PaletteHelper _paletteHelper = new();

    /// <summary>
    /// Sets the application theme to dark or light mode.
    /// </summary>
    /// <param name="isDark">
    /// <c>true</c> to enable dark theme; <c>false</c> to enable light theme.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method retrieves the current theme, modifies its base theme setting,
    /// and applies the updated theme. The change is immediately reflected across
    /// all Material Design components in the application.
    /// </para>
    /// <para>
    /// Theme changes affect:
    /// <list type="bullet">
    ///   <item><description>Background and surface colors</description></item>
    ///   <item><description>Text and icon colors</description></item>
    ///   <item><description>Elevation and shadow rendering</description></item>
    ///   <item><description>Border and divider colors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The theme persists for the lifetime of the application session but is not
    /// saved to disk. User preference should be stored separately if persistence
    /// is required.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example usage in a view model:
    /// <code>
    /// // Switch to dark mode
    /// themeService.SetDark(true);
    /// 
    /// // Switch to light mode
    /// themeService.SetDark(false);
    /// </code>
    /// </example>
    public void SetDark(bool isDark)
    {
        var theme = _paletteHelper.GetTheme();

        theme.SetBaseTheme(
            isDark
                ? BaseTheme.Dark
                : BaseTheme.Light);

        _paletteHelper.SetTheme(theme);
    }
}