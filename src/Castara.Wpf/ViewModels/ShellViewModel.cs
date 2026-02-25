using System.ComponentModel;
using System.Windows.Media;
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
///   <item><description>Theme switching (Dark/Light mode) across Material Design and OxyPlot components</description></item>
///   <item><description>View hosting and navigation</description></item>
///   <item><description>Application status display with visual indicators</description></item>
///   <item><description>Coordination between multiple view models and services</description></item>
/// </list>
/// </para>
/// <para>
/// This view model follows the MVVM pattern and implements <see cref="INotifyPropertyChanged"/>
/// to support WPF data binding. It acts as the DataContext for the main window shell.
/// </para>
/// </remarks>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly CalculationsViewModel _calculationsViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class with the required services.
    /// </summary>
    /// <param name="themeService">
    /// The theme service responsible for Material Design theme switching.
    /// </param>
    /// <param name="statusService">
    /// The status service that manages application-wide status messages and levels.
    /// </param>
    /// <param name="calculationsView">
    /// The calculations view to be hosted in the shell.
    /// </param>
    /// <param name="calculationsViewModel">
    /// The calculations view model that manages the calculations view's logic and state.
    /// </param>
    /// <remarks>
    /// <para>
    /// The constructor performs the following initialization:
    /// <list type="number">
    ///   <item><description>Wires up the calculations view with its view model</description></item>
    ///   <item><description>Sets the calculations view as the current hosted view</description></item>
    ///   <item><description>Subscribes to status service changes for reactive status updates</description></item>
    ///   <item><description>Initializes dark mode theme across both Material Design and OxyPlot</description></item>
    ///   <item><description>Sets the initial status to "Ready"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Theme synchronization ensures both Material Design UI components and OxyPlot charts
    /// use consistent theming, providing a cohesive visual experience.
    /// </para>
    /// </remarks>
    public ShellViewModel(
        ThemeService themeService,
        IStatusService statusService,
        Views.CalculationsView calculationsView,
        CalculationsViewModel calculationsViewModel)
    {
        _themeService = themeService;
        _statusService = statusService;
        _calculationsViewModel = calculationsViewModel;

        calculationsView.DataContext = calculationsViewModel;
        CurrentView = calculationsView;

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

        IsDarkMode = true;

        // Synchronize both theme systems at startup
        _themeService.SetDark(true);
        _calculationsViewModel.SetTheme(true);

        _statusService.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Theme toggle
    // -----------------------------

    private bool _isDarkMode;

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled for the application.
    /// </summary>
    /// <value>
    /// <c>true</c> if dark mode is enabled; otherwise, <c>false</c> for light mode.
    /// </value>
    /// <remarks>
    /// <para>
    /// When this property changes, it synchronizes the theme across multiple systems:
    /// <list type="bullet">
    ///   <item><description>Material Design UI components (via <see cref="ThemeService"/>)</description></item>
    ///   <item><description>OxyPlot chart themes (via <see cref="CalculationsViewModel"/>)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This ensures a consistent visual experience throughout the application, with all
    /// UI elements and visualizations respecting the selected theme.
    /// </para>
    /// </remarks>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;

            _isDarkMode = value;

            // Update Material Design theme
            _themeService.SetDark(value);

            // Update OxyPlot theme in the calculations VM
            _calculationsViewModel.SetTheme(value);

            Notify(nameof(IsDarkMode));
        }
    }

    // -----------------------------
    // Hosted view
    // -----------------------------

    private object? _currentView;

    /// <summary>
    /// Gets the current view being hosted in the shell's content area.
    /// </summary>
    /// <value>
    /// The current view instance, typically a <see cref="UserControl"/> or other WPF visual element.
    /// </value>
    /// <remarks>
    /// This property enables view switching and navigation within the application.
    /// Currently, the application hosts the calculations view, but this design allows
    /// for future expansion to multiple views or navigation scenarios.
    /// </remarks>
    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            if (Equals(_currentView, value)) return;
            _currentView = value;
            Notify(nameof(CurrentView));
        }
    }

    // -----------------------------
    // Status bindings
    // -----------------------------

    /// <summary>
    /// Gets the left-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current status message (e.g., "Ready", "Calculating...", "Error occurred").
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes.
    /// </remarks>
    public string StatusLeftText => _statusService.Current.LeftText;

    /// <summary>
    /// Gets the right-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current contextual information (e.g., "SQLite • Local", connection status).
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes.
    /// </remarks>
    public string StatusRightText => _statusService.Current.RightText;

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
    /// These colors follow iOS/macOS system color conventions for status indicators,
    /// providing intuitive visual feedback about application state.
    /// </remarks>
    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35C759")),
            AppStatusLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
            AppStatusLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),
            _ => Brushes.Gray
        };

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    /// <remarks>
    /// This helper method simplifies property change notifications throughout the view model,
    /// ensuring the UI updates reactively when properties change.
    /// </remarks>
    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}