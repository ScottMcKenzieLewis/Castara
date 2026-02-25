using System.ComponentModel;
using System.Windows.Media;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The shell view model that manages application-level concerns including navigation,
/// theming, and status display for the Castara application.
/// </summary>
/// <remarks>
/// <para>
/// The shell view model serves as the top-level coordinator for the application,
/// managing:
/// <list type="bullet">
///   <item><description>Theme switching (Dark/Light mode) across Material Design and custom visualizations</description></item>
///   <item><description>View hosting and navigation</description></item>
///   <item><description>Application status display with visual indicators</description></item>
///   <item><description>Coordination between multiple view models and services</description></item>
/// </list>
/// </para>
/// <para>
/// This view model follows the MVVM pattern and implements <see cref="INotifyPropertyChanged"/>
/// to support WPF data binding. It acts as the DataContext for the main window shell.
/// </para>
/// <para>
/// <strong>Theme Coordination:</strong> The shell provides a single synchronization point
/// for theme changes, ensuring both Material Design UI components and custom visualizations
/// (via <see cref="IThemeAware"/>) update consistently.
/// </para>
/// </remarks>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly IThemeAware _calculationsViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class with the required services.
    /// </summary>
    /// <param name="themeService">
    /// The theme service responsible for Material Design theme switching.
    /// </param>
    /// <param name="statusService">
    /// The status service that manages application-wide status messages and levels.
    /// </param>
    /// <param name="calculationsViewModel">
    /// The calculations view model that implements <see cref="IThemeAware"/> for chart theme coordination.
    /// </param>
    /// <remarks>
    /// <para>
    /// The constructor performs the following initialization:
    /// <list type="number">
    ///   <item><description>Sets the calculations view model as the current hosted view</description></item>
    ///   <item><description>Subscribes to status service changes for reactive status updates</description></item>
    ///   <item><description>Initializes dark mode theme via the <see cref="IsDarkMode"/> property setter</description></item>
    ///   <item><description>Sets the initial status to "Ready"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Theme initialization happens exclusively through the
    /// <see cref="IsDarkMode"/> property setter to ensure proper synchronization across
    /// all theme-aware components from the start.
    /// </para>
    /// </remarks>
    public ShellViewModel(
        IThemeService themeService,
        IStatusService statusService,
        IThemeAware calculationsViewModel)
    {
        _themeService = themeService;
        _statusService = statusService;
        _calculationsViewModel = calculationsViewModel;

        CurrentViewModel = calculationsViewModel;

        // Subscribe to status changes for reactive UI updates
        _statusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IStatusService.Current))
            {
                Notify(nameof(StatusLeftText));
                Notify(nameof(StatusRightText));
                Notify(nameof(StatusBrush));
            }
        };

        // IMPORTANT:
        // Theme initialization happens ONLY here via the property setter
        // to ensure synchronization across all theme-aware components
        IsDarkMode = true;

        // Set initial application status
        _statusService.Set(
            AppStatusLevel.Ok,
            "Ready",
            "Ready for Calculation");
    }

    // -------------------------------------------------
    // Theme Toggle
    // -------------------------------------------------

    private bool _isDarkMode;

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled for the application.
    /// </summary>
    /// <value>
    /// <c>true</c> if dark mode is enabled; otherwise, <c>false</c> for light mode.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property serves as the single synchronization point for theme changes across
    /// the entire application. When the value changes:
    /// <list type="number">
    ///   <item><description>Material Design UI components are updated via <see cref="IThemeService"/></description></item>
    ///   <item><description>Custom visualizations (OxyPlot charts) are updated via <see cref="IThemeAware"/></description></item>
    ///   <item><description>Property change notification is raised for UI binding</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This centralized approach ensures consistent visual appearance across all UI
    /// elements and prevents theme synchronization issues between different subsystems.
    /// </para>
    /// </remarks>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value)
                return;

            _isDarkMode = value;

            // Single synchronization point for all theme changes
            _themeService.SetDark(value);
            _calculationsViewModel.SetTheme(value);

            Notify(nameof(IsDarkMode));
        }
    }

    // -------------------------------------------------
    // Hosted ViewModel
    // -------------------------------------------------

    private object? _currentViewModel;

    /// <summary>
    /// Gets the current view model being hosted in the shell's content area.
    /// </summary>
    /// <value>
    /// The current view model instance, typically implementing <see cref="IThemeAware"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property enables view switching and navigation within the application.
    /// Currently, the application hosts the calculations view model, but this design
    /// allows for future expansion to multiple views or navigation scenarios.
    /// </para>
    /// <para>
    /// The hosted view model is set via dependency injection in the constructor,
    /// ensuring proper initialization and testability.
    /// </para>
    /// </remarks>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (Equals(_currentViewModel, value))
                return;

            _currentViewModel = value;
            Notify(nameof(CurrentViewModel));
        }
    }

    // -------------------------------------------------
    // Status Bindings
    // -------------------------------------------------

    /// <summary>
    /// Gets the left-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current status message (e.g., "Ready", "Calculated", "Check inputs").
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes through the subscribed property change handler.
    /// </remarks>
    public string StatusLeftText
        => _statusService.Current.LeftText;

    /// <summary>
    /// Gets the right-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current contextual information (e.g., "Ready for Calculation", "2 risk(s)").
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes through the subscribed property change handler.
    /// </remarks>
    public string StatusRightText
        => _statusService.Current.RightText;

    /// <summary>
    /// Gets the brush used to color the status indicator based on the current status level.
    /// </summary>
    /// <value>
    /// A <see cref="SolidColorBrush"/> with colors indicating status:
    /// <list type="bullet">
    ///   <item><description>Green (#35C759) for <see cref="AppStatusLevel.Ok"/></description></item>
    ///   <item><description>Yellow (#FFCC00) for <see cref="AppStatusLevel.Warning"/></description></item>
    ///   <item><description>Red (#FF3B30) for <see cref="AppStatusLevel.Error"/></description></item>
    ///   <item><description>Gray for unknown states</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// These colors follow iOS/macOS system color conventions for status indicators,
    /// providing intuitive visual feedback about application state.
    /// </para>
    /// <para>
    /// A new brush instance is created each time this property is accessed. For
    /// performance-critical scenarios, consider caching the brushes, though current
    /// usage patterns make this optimization unnecessary.
    /// </para>
    /// </remarks>
    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok =>
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#35C759")),

            AppStatusLevel.Warning =>
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFCC00")),

            AppStatusLevel.Error =>
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF3B30")),

            _ => Brushes.Gray
        };

    // -------------------------------------------------
    // Notify Helper
    // -------------------------------------------------

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    /// <remarks>
    /// This helper method simplifies property change notifications throughout the view model,
    /// ensuring the UI updates reactively when properties change.
    /// </remarks>
    private void Notify(string name)
        => PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(name));
}