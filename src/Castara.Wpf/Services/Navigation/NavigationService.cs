using Castara.Wpf.Models;
using Castara.Wpf.Services.Navigation;
using Castara.Wpf.Views;
using System.ComponentModel;

namespace Castara.Wpf.Services.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly DashboardView _dashboardView;
    private readonly ProfilesView _profilesView;
    private readonly CalculationsView _calculationsView;
    private readonly RiskFlagsView _riskFlagsView;
    private readonly SettingsView _settingsView;

    public event PropertyChangedEventHandler? PropertyChanged;

    private object _currentView;
    public object CurrentView
    {
        get => _currentView;
        private set
        {
            if (ReferenceEquals(_currentView, value)) return;
            _currentView = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentView)));
        }
    }

    private string _pageTitle = "Dashboard";
    public string PageTitle
    {
        get => _pageTitle;
        private set
        {
            if (_pageTitle == value) return;
            _pageTitle = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageTitle)));
        }
    }

    public NavigationService(
        DashboardView dashboardView,
        ProfilesView profilesView,
        CalculationsView calculationsView,
        RiskFlagsView riskFlagsView,
        SettingsView settingsView)
    {
        _dashboardView = dashboardView;
        _profilesView = profilesView;
        _calculationsView = calculationsView;
        _riskFlagsView = riskFlagsView;
        _settingsView = settingsView;

        // Default route
        _currentView = _dashboardView;
        _pageTitle = "Dashboard";
    }

    public void Navigate(NavRoute route)
    {
        switch (route)
        {
            case NavRoute.Profiles:
                CurrentView = _profilesView;
                PageTitle = "Composition Profiles";
                break;

            case NavRoute.Calculations:
                CurrentView = _calculationsView;
                PageTitle = "Calculations";
                break;

            case NavRoute.RiskFlags:
                CurrentView = _riskFlagsView;
                PageTitle = "Risk Flags";
                break;

            case NavRoute.Settings:
                CurrentView = _settingsView;
                PageTitle = "Settings";
                break;

            default:
                CurrentView = _dashboardView;
                PageTitle = "Dashboard";
                break;
        }
    }
}