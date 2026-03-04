using System;
using System.ComponentModel;
using System.Windows.Media;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The shell view model that manages application-level concerns including navigation,
/// theming, unit system management, status display, and log viewing coordination.
/// </summary>
/// <remarks>
/// <para>
/// The shell view model serves as the top-level coordinator for the Castara application,
/// managing multiple cross-cutting concerns:
/// <list type="bullet">
///   <item><description>Theme switching (Dark/Light mode) across Material Design and custom visualizations</description></item>
///   <item><description>Unit system coordination (Standard/American) across calculation views</description></item>
///   <item><description>View hosting and navigation (currently single-view, extensible for multi-view)</description></item>
///   <item><description>Application status display with visual indicators</description></item>
///   <item><description>Log viewer child view model management</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Architecture Pattern:</strong> This view model follows the composite pattern, hosting
/// child view models (LogViewerViewModel, CalculationsViewModel) while coordinating shared
/// concerns like theming and unit systems through interface-based abstractions
/// (<see cref="IThemeAware"/>, <see cref="IUnitAware"/>).
/// </para>
/// <para>
/// <strong>Coordination Strategy:</strong> The shell provides single synchronization points for:
/// <list type="bullet">
///   <item><description>Theme changes via <see cref="IsDarkMode"/> property</description></item>
///   <item><description>Unit system changes via <see cref="UnitSystem"/> property</description></item>
///   <item><description>Status updates via reactive binding to <see cref="IStatusService"/></description></item>
/// </list>
/// This ensures consistent state across all view models and prevents synchronization issues.
/// </para>
/// <para>
/// This view model implements <see cref="INotifyPropertyChanged"/> to support WPF data binding
/// and acts as the DataContext for the main window shell.
/// </para>
/// </remarks>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    // ============================================================
    // Fields
    // ============================================================

    private readonly IThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly IThemeAware _themeAware;
    private readonly IUnitAware _unitAware;

    private bool _isDarkMode;
    private object? _currentViewModel;
    private UnitSystem? _unitSystem;

    // ============================================================
    // Events
    // ============================================================

    /// <summary>
    /// Occurs when a property value changes, supporting WPF data binding.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ============================================================
    // Constructor
    // ============================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class with required services and child view models.
    /// </summary>
    /// <param name="themeService">
    /// The theme service responsible for Material Design theme switching.
    /// </param>
    /// <param name="statusService">
    /// The status service that manages application-wide status messages and levels.
    /// </param>
    /// <param name="themeAware">
    /// The theme-aware view model (typically CalculationsViewModel) for chart theme coordination.
    /// </param>
    /// <param name="unitAware">
    /// The unit-aware view model (typically CalculationsViewModel) for unit system coordination.
    /// </param>
    /// <param name="logViewer">
    /// The log viewer view model for diagnostic log display and management.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor performs the following initialization sequence:
    /// <list type="number">
    ///   <item><description>Stores service and child view model references</description></item>
    ///   <item><description>Sets the calculations view model as the current hosted view</description></item>
    ///   <item><description>Subscribes to status service changes for reactive status updates</description></item>
    ///   <item><description>Initializes unit system to Standard (propagates to calculations VM)</description></item>
    ///   <item><description>Initializes dark mode theme (synchronizes Material Design and charts)</description></item>
    ///   <item><description>Sets initial status to "Ready for Calculation"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Theme and unit system initialization happens exclusively through
    /// property setters (<see cref="IsDarkMode"/>, <see cref="UnitSystem"/>) to ensure proper
    /// synchronization across all components from the start.
    /// </para>
    /// </remarks>
    public ShellViewModel(
        IThemeService themeService,
        IStatusService statusService,
        IThemeAware themeAware,
        IUnitAware unitAware,
        LogViewerViewModel logViewer)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _themeAware = themeAware ?? throw new ArgumentNullException(nameof(themeAware));
        _unitAware = unitAware ?? throw new ArgumentNullException(nameof(unitAware));
        LogViewerViewModel = logViewer ?? throw new ArgumentNullException(nameof(logViewer));

        // Host the main content (currently the calculations VM)
        CurrentViewModel = themeAware;

        // Subscribe to status changes for reactive UI bindings
        _statusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IStatusService.Current))
            {
                Notify(nameof(StatusLeftText));
                Notify(nameof(StatusRightText));
                Notify(nameof(StatusBrush));
            }
        };

        // Initialize unit system via property setter for proper propagation
        UnitSystem = UnitSystem.Standard;

        // IMPORTANT:
        // Theme initialization happens ONLY here via the property setter
        // to ensure synchronization across all theme-aware components
        IsDarkMode = true;

        // Set initial application status
        _statusService.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // ============================================================
    // Properties - Child ViewModels
    // ============================================================

    /// <summary>
    /// Gets the log viewer view model for diagnostic log display and management.
    /// </summary>
    /// <value>
    /// The <see cref="ViewModels.LogViewerViewModel"/> instance provided during construction.
    /// </value>
    /// <remarks>
    /// <para>
    /// This child view model is owned by the shell but operates independently with its own:
    /// <list type="bullet">
    ///   <item><description>Dialog visibility state (<see cref="ViewModels.LogViewerViewModel.IsOpen"/>)</description></item>
    ///   <item><description>Log filtering and search functionality</description></item>
    ///   <item><description>Selection and clipboard commands</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The log viewer is bound in the MainWindow XAML via DialogHost for modal overlay display.
    /// </para>
    /// </remarks>
    public LogViewerViewModel LogViewerViewModel { get; }

    // ============================================================
    // Properties - View Hosting
    // ============================================================

    /// <summary>
    /// Gets the current view model being hosted in the shell's content area.
    /// </summary>
    /// <value>
    /// The current view model instance, typically implementing both <see cref="IThemeAware"/>
    /// and <see cref="IUnitAware"/>.
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

    // ============================================================
    // Properties - Theme
    // ============================================================

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
    /// elements and prevents theme synchronization issues between Material Design and
    /// custom chart components.
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
            _themeAware.SetTheme(value);

            Notify(nameof(IsDarkMode));
        }
    }

    // ============================================================
    // Properties - Unit System
    // ============================================================

    /// <summary>
    /// Gets or sets the current unit system for measurements throughout the application.
    /// </summary>
    /// <value>
    /// A <see cref="Models.UnitSystem"/> enum value (Standard or AmericanStandard).
    /// </value>
    /// <remarks>
    /// <para>
    /// This property serves as the single synchronization point for unit system changes.
    /// When the value changes:
    /// <list type="number">
    ///   <item><description>The unit system is propagated to <see cref="IUnitAware"/> components</description></item>
    ///   <item><description>Related UI properties are updated (<see cref="IsAmericanStandard"/>, text properties)</description></item>
    ///   <item><description>All input/output values are converted to the selected unit system</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Unit systems supported:
    /// <list type="bullet">
    ///   <item><description><strong>Standard:</strong> SI units (mm, °C, etc.)</description></item>
    ///   <item><description><strong>AmericanStandard:</strong> US customary units (inches, °F, etc.)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public UnitSystem UnitSystem
    {
        get => _unitSystem ?? UnitSystem.Standard;
        set
        {
            if (_unitSystem.HasValue && _unitSystem.Value == value) return;

            _unitSystem = value;
            _unitAware.UnitSystem = value;

            Notify(nameof(UnitSystem));
            Notify(nameof(IsAmericanStandard));
            Notify(nameof(UnitSystemLeftText));
            Notify(nameof(UnitSystemRightText));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether American Standard units are currently selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if American Standard units are active; <c>false</c> for Standard (SI) units.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides a boolean convenience wrapper around <see cref="UnitSystem"/>
    /// for easier binding to toggle controls in XAML. Setting this property updates the
    /// underlying <see cref="UnitSystem"/> enum value.
    /// </para>
    /// <para>
    /// The property is bound to a ToggleButton in the MainWindow toolbar for easy unit switching.
    /// </para>
    /// </remarks>
    public bool IsAmericanStandard
    {
        get => UnitSystem == UnitSystem.AmericanStandard;
        set => UnitSystem = value ? UnitSystem.AmericanStandard : UnitSystem.Standard;
    }

    /// <summary>
    /// Gets the left-side text label for the unit system toggle control.
    /// </summary>
    /// <value>
    /// Always returns "Units".
    /// </value>
    /// <remarks>
    /// This property provides a consistent label for the unit system toggle section in the UI.
    /// </remarks>
    public string UnitSystemLeftText => "Units";

    /// <summary>
    /// Gets the right-side text indicating the currently selected unit system.
    /// </summary>
    /// <value>
    /// "American" if American Standard units are active; otherwise "Standard".
    /// </value>
    /// <remarks>
    /// This property is bound to the UI to display the current unit system selection,
    /// updating automatically when <see cref="UnitSystem"/> changes.
    /// </remarks>
    public string UnitSystemRightText => IsAmericanStandard ? "American" : "Standard";

    // ============================================================
    // Properties - Status
    // ============================================================

    /// <summary>
    /// Gets the left-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current status message (e.g., "Ready", "Calculated", "Check inputs", "Calculation failed").
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes through the subscribed property change handler in the constructor.
    /// </remarks>
    public string StatusLeftText => _statusService.Current.LeftText;

    /// <summary>
    /// Gets the right-side status text to be displayed in the application status bar.
    /// </summary>
    /// <value>
    /// The current contextual information (e.g., "Ready for Calculation", "2 risk(s)", "OK").
    /// </value>
    /// <remarks>
    /// This property is reactive and automatically updates when the status service's
    /// current status changes through the subscribed property change handler in the constructor.
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
    /// <para>
    /// These colors follow iOS/macOS system color conventions for status indicators,
    /// providing intuitive visual feedback about application state:
    /// <list type="bullet">
    ///   <item><description><strong>Green:</strong> Success, ready state, normal operation</description></item>
    ///   <item><description><strong>Yellow:</strong> Warnings, risks detected, attention needed</description></item>
    ///   <item><description><strong>Red:</strong> Errors, validation failures, critical issues</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// A new brush instance is created each time this property is accessed. For
    /// performance-critical scenarios, consider caching the brushes, though current
    /// usage patterns (status updates are infrequent) make this optimization unnecessary.
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

    // ============================================================
    // Notify helper
    // ============================================================

    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}