using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The shell view model that manages application-level concerns including navigation,
/// theming, status display, and log viewing for the Castara application.
/// </summary>
/// <remarks>
/// <para>
/// The shell view model serves as the top-level coordinator for the application,
/// managing:
/// <list type="bullet">
///   <item><description>Theme switching (Dark/Light mode) across Material Design and custom visualizations</description></item>
///   <item><description>View hosting and navigation</description></item>
///   <item><description>Application status display with visual indicators</description></item>
///   <item><description>Log viewing with filtering, searching, and sorting capabilities</description></item>
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
/// <para>
/// <strong>Log Management:</strong> The shell exposes a filterable, searchable view of
/// application logs through <see cref="LogEntriesView"/>, supporting real-time diagnostics
/// and troubleshooting.
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
    /// Gets the observable log store for application diagnostics and telemetry.
    /// </summary>
    /// <value>
    /// The log store containing all application log entries with thread-safe access.
    /// </value>
    public IObservableLogStore LogStore { get; }

    // ============================================================
    // Log Dialog State and View
    // ============================================================

    private bool _isLogsOpen;

    /// <summary>
    /// Gets or sets a value indicating whether the logs dialog is currently open.
    /// </summary>
    /// <value>
    /// <c>true</c> if the logs dialog is open; otherwise, <c>false</c>.
    /// </value>
    public bool IsLogsOpen
    {
        get => _isLogsOpen;
        set
        {
            if (_isLogsOpen == value) return;
            _isLogsOpen = value;
            Notify(nameof(IsLogsOpen));
        }
    }

    /// <summary>
    /// Gets the total number of log entries currently in the store.
    /// </summary>
    /// <value>
    /// The count of all log entries, regardless of current filter settings.
    /// </value>
    public int LogCount => LogStore.Entries.Count;

    /// <summary>
    /// Gets a value indicating whether any log entries exist in the store.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one log entry exists; otherwise, <c>false</c>.
    /// </value>
    public bool HasLogs => LogCount > 0;

    private string _logSearchText = string.Empty;

    /// <summary>
    /// Gets or sets the search text for filtering log entries.
    /// </summary>
    /// <value>
    /// A string to search within log message, category, and exception text.
    /// Search is case-insensitive.
    /// </value>
    /// <remarks>
    /// When this property changes, the <see cref="LogEntriesView"/> is automatically
    /// refreshed to apply the new filter criteria.
    /// </remarks>
    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_logSearchText, next, StringComparison.Ordinal)) return;
            _logSearchText = next;
            Notify(nameof(LogSearchText));
            LogEntriesView.Refresh();
        }
    }

    private LogLevel? _selectedLogLevel;

    /// <summary>
    /// Gets or sets the minimum log level to display (inclusive filter).
    /// </summary>
    /// <value>
    /// A <see cref="LogLevel"/> value, or <c>null</c> to show all levels.
    /// </value>
    /// <remarks>
    /// This property provides exact level matching (not minimum level filtering).
    /// When set, only entries matching this specific level are shown.
    /// Set to <c>null</c> to display all log levels.
    /// </remarks>
    public LogLevel? SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            if (_selectedLogLevel == value) return;
            _selectedLogLevel = value;
            Notify(nameof(SelectedLogLevel));

            // Keep SelectedLogLevelOption in sync for UI binding
            SelectedLogLevelOption = LogLevelOptions.First(o => o.Level == value);

            LogEntriesView.Refresh();
        }
    }

    private LogLevelOption _selectedLogLevelOption = LogLevelOption.All;

    /// <summary>
    /// Gets or sets the selected log level option for UI binding.
    /// </summary>
    /// <value>
    /// A <see cref="LogLevelOption"/> representing the current filter selection.
    /// </value>
    /// <remarks>
    /// This property is synchronized with <see cref="SelectedLogLevel"/> and provides
    /// a display-friendly option for ComboBox binding.
    /// </remarks>
    public LogLevelOption SelectedLogLevelOption
    {
        get => _selectedLogLevelOption;
        set
        {
            if (Equals(_selectedLogLevelOption, value)) return;
            _selectedLogLevelOption = value;
            Notify(nameof(SelectedLogLevelOption));

            // Drive filter through canonical SelectedLogLevel
            _selectedLogLevel = value.Level;
            Notify(nameof(SelectedLogLevel));

            LogEntriesView.Refresh();
        }
    }

    private LogEntry? _selectedLogEntry;

    /// <summary>
    /// Gets or sets the currently selected log entry in the log viewer.
    /// </summary>
    /// <value>
    /// The selected <see cref="LogEntry"/>, or <c>null</c> if no entry is selected.
    /// </value>
    /// <remarks>
    /// This property is typically bound to the SelectedItem of a DataGrid or ListBox
    /// to display detailed information about a specific log entry.
    /// </remarks>
    public LogEntry? SelectedLogEntry
    {
        get => _selectedLogEntry;
        set
        {
            if (Equals(_selectedLogEntry, value)) return;
            _selectedLogEntry = value;
            Notify(nameof(SelectedLogEntry));
        }
    }

    /// <summary>
    /// Gets a filtered and sorted collection view over the log entries for UI binding.
    /// </summary>
    /// <value>
    /// An <see cref="ICollectionView"/> that supports filtering, sorting, and change notification.
    /// </value>
    /// <remarks>
    /// <para>
    /// This view is configured with:
    /// <list type="bullet">
    ///   <item><description>Filtering by log level and search text via <see cref="LogFilter"/></description></item>
    ///   <item><description>Descending sort by timestamp (newest entries first)</description></item>
    ///   <item><description>Automatic refresh when filter criteria change</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Bind DataGrid or ListBox controls directly to this property for real-time
    /// filtered log display.
    /// </para>
    /// </remarks>
    public ICollectionView LogEntriesView { get; }

    /// <summary>
    /// Gets the available log level filter options for UI binding.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="LogLevelOption"/> instances including "All" and each log level.
    /// </value>
    /// <remarks>
    /// Bind a ComboBox's ItemsSource to this property with DisplayMemberPath="Display"
    /// for a user-friendly log level filter.
    /// </remarks>
    public IReadOnlyList<LogLevelOption> LogLevelOptions { get; } = new[]
    {
        LogLevelOption.All,
        new LogLevelOption("Trace", LogLevel.Trace),
        new LogLevelOption("Debug", LogLevel.Debug),
        new LogLevelOption("Information", LogLevel.Information),
        new LogLevelOption("Warning", LogLevel.Warning),
        new LogLevelOption("Error", LogLevel.Error),
        new LogLevelOption("Critical", LogLevel.Critical),
    };

    /// <summary>
    /// Gets the command to show the logs dialog.
    /// </summary>
    /// <value>
    /// A command that sets <see cref="IsLogsOpen"/> to <c>true</c>.
    /// </value>
    public ICommand ShowLogsCommand { get; }

    /// <summary>
    /// Gets the command to close the logs dialog.
    /// </summary>
    /// <value>
    /// A command that sets <see cref="IsLogsOpen"/> to <c>false</c>.
    /// </value>
    public ICommand CloseLogsCommand { get; }

    /// <summary>
    /// Gets the command to clear all log entries from the store.
    /// </summary>
    /// <value>
    /// A command that clears the log store and resets selection and counts.
    /// </value>
    public ICommand ClearLogsCommand { get; }

    // ============================================================
    // Construction
    // ============================================================

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
    /// <param name="logStore">
    /// The observable log store for application diagnostics and telemetry.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor performs the following initialization:
    /// <list type="number">
    ///   <item><description>Sets the calculations view model as the current hosted view</description></item>
    ///   <item><description>Configures the log entries collection view with filtering and sorting</description></item>
    ///   <item><description>Subscribes to log store and status service changes for reactive updates</description></item>
    ///   <item><description>Initializes commands for log management</description></item>
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
        IThemeAware calculationsViewModel,
        IObservableLogStore logStore)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _calculationsViewModel = calculationsViewModel ?? throw new ArgumentNullException(nameof(calculationsViewModel));
        LogStore = logStore ?? throw new ArgumentNullException(nameof(logStore));

        CurrentViewModel = calculationsViewModel;

        // Build the collection view over the store entries
        LogEntriesView = CollectionViewSource.GetDefaultView(LogStore.Entries);
        LogEntriesView.Filter = LogFilter;

        // Sort newest entries first
        LogEntriesView.SortDescriptions.Clear();
        LogEntriesView.SortDescriptions.Add(
            new SortDescription(nameof(LogEntry.Timestamp), ListSortDirection.Descending));

        // React to log collection changes
        if (LogStore.Entries is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += (_, __) =>
            {
                Notify(nameof(LogCount));
                Notify(nameof(HasLogs));

                // Keep view current with filtering/sorting
                LogEntriesView.Refresh();
            };
        }

        // Default filter: All levels
        SelectedLogLevelOption = LogLevelOption.All;

        // Initialize commands
        ShowLogsCommand = new RelayCommand(() => IsLogsOpen = true);
        CloseLogsCommand = new RelayCommand(() => IsLogsOpen = false);

        ClearLogsCommand = new RelayCommand(() =>
        {
            LogStore.Clear();
            SelectedLogEntry = null;

            Notify(nameof(LogCount));
            Notify(nameof(HasLogs));
            LogEntriesView.Refresh();
        });

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
        _statusService.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation");
    }

    // ============================================================
    // Filtering Logic
    // ============================================================

    /// <summary>
    /// Filters log entries based on selected log level and search text.
    /// </summary>
    /// <param name="obj">The object to filter, expected to be a <see cref="LogEntry"/>.</param>
    /// <returns>
    /// <c>true</c> if the log entry passes all filter criteria; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Filter criteria applied in order:
    /// <list type="number">
    ///   <item><description>Type check: Must be a <see cref="LogEntry"/></description></item>
    ///   <item><description>Log level: Must match <see cref="SelectedLogLevel"/> if not null</description></item>
    ///   <item><description>Search text: Must be found in message, category, or exception text (case-insensitive)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is used as the filter predicate for <see cref="LogEntriesView"/>.
    /// </para>
    /// </remarks>
    private bool LogFilter(object obj)
    {
        if (obj is not LogEntry entry)
            return false;

        // Exact level filter (not minimum level)
        var level = SelectedLogLevelOption?.Level;
        if (level is { } l && entry.Level != l)
            return false;

        // Search text filter
        var q = (LogSearchText ?? string.Empty).Trim();
        if (q.Length == 0)
            return true;

        return Contains(entry.Message, q)
            || Contains(entry.Category, q)
            || (entry.Exception is not null && Contains(entry.Exception.ToString(), q));
    }

    /// <summary>
    /// Performs case-insensitive substring search.
    /// </summary>
    /// <param name="haystack">The string to search within.</param>
    /// <param name="needle">The substring to search for.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="needle"/> is found within <paramref name="haystack"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool Contains(string? haystack, string needle)
        => !string.IsNullOrEmpty(haystack)
           && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// Represents a log level filter option for UI binding.
    /// </summary>
    /// <param name="Display">The display text for the option.</param>
    /// <param name="Level">
    /// The associated <see cref="LogLevel"/>, or <c>null</c> for "All" option.
    /// </param>
    public sealed record LogLevelOption(string Display, LogLevel? Level)
    {
        /// <summary>
        /// Gets the "All" log level option that shows entries of all levels.
        /// </summary>
        public static LogLevelOption All { get; } = new("All", null);

        /// <summary>
        /// Returns the display text for this option.
        /// </summary>
        /// <returns>The display text.</returns>
        public override string ToString() => Display;
    }

    // ============================================================
    // Theme Toggle
    // ============================================================

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

    // ============================================================
    // Hosted ViewModel
    // ============================================================

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

    // ============================================================
    // Status Bindings
    // ============================================================

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

    // ============================================================
    // Notify Helper
    // ============================================================

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