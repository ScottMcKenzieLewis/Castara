using Castara.Wpf.Models;
using Castara.Wpf.Services.Navigation;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.Views;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace Castara.Wpf.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ThemeService _themeService;
    private readonly IStatusService _statusService;
    private readonly INavigationService _nav;

    private readonly DashboardView _dashboardView;
    private readonly ProfilesView _profilesView;
    private readonly CalculationsView _calculationsView;
    private readonly RiskFlagsView _riskFlagsView;
    private readonly SettingsView _settingsView;

    public ShellViewModel(
        ThemeService themeService,
        IStatusService statusService,
        INavigationService navService,
        DashboardView dashboardView,
        ProfilesView profilesView,
        CalculationsView calculationsView,
        RiskFlagsView riskFlagsView,
        SettingsView settingsView)
    {
        _themeService = themeService;
        _statusService = statusService;
        _nav = navService;

        _dashboardView = dashboardView;
        _profilesView = profilesView;
        _calculationsView = calculationsView;
        _riskFlagsView = riskFlagsView;
        _settingsView = settingsView;

        // Default page
        CurrentView = _dashboardView;
        PageTitle = "Dashboard";

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

        _statusService.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");

        _nav.Navigate(NavRoute.Dashboard);

    }

    // -----------------------------
    // Current View
    // -----------------------------

    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            Notify(nameof(CurrentView));
        }
    }

    // -----------------------------
    // Navigation selection
    // -----------------------------

    private ListBoxItem? _selectedNavItem;

    public ListBoxItem? SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            _selectedNavItem = value;
            Notify(nameof(SelectedNavItem));

            if (value?.Tag is string route)
                Navigate(route);
        }
    }

    // -----------------------------
    // Page Title
    // -----------------------------

    private string _pageTitle = "Dashboard";

    public string PageTitle
    {
        get => _pageTitle;
        set
        {
            _pageTitle = value;
            Notify(nameof(PageTitle));
        }
    }

    // -----------------------------
    // Dark Mode Toggle
    // -----------------------------

    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;

            _isDarkMode = value;
            _themeService.SetDark(value);
            Notify(nameof(IsDarkMode));
        }
    }

    // -----------------------------
    // Status Bar (bound by MainWindow.xaml)
    // -----------------------------

    public string StatusLeftText => _statusService.Current.LeftText;

    public string StatusRightText => _statusService.Current.RightText;

    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35C759")),
            AppStatusLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
            AppStatusLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),
            _ => Brushes.Gray
        };

    // -----------------------------
    // Navigation Logic
    // -----------------------------

    private void Navigate(string route)
    {
        switch (route)
        {
            case "Profiles":
                CurrentView = _profilesView;
                PageTitle = "Composition Profiles";
                _statusService.Set(AppStatusLevel.Ok, "Ready", "Profiles");
                break;

            case "Calculations":
                CurrentView = _calculationsView;
                PageTitle = "Calculations";
                _statusService.Set(AppStatusLevel.Ok, "Ready", "Calculations");
                break;

            case "RiskFlags":
                CurrentView = _riskFlagsView;
                PageTitle = "Risk Flags";
                _statusService.Set(AppStatusLevel.Ok, "Ready", "Risk Flags");
                break;

            case "Settings":
                CurrentView = _settingsView;
                PageTitle = "Settings";
                _statusService.Set(AppStatusLevel.Ok, "Ready", "Settings");
                break;

            default:
                CurrentView = _dashboardView;
                PageTitle = "Dashboard";
                _statusService.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
                break;
        }
    }

    // -----------------------------
    // Property Changed Helper
    // -----------------------------

    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}