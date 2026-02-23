using Castara.Wpf.Services.Navigation;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Castara.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Castara.Wpf;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _services = services.BuildServiceProvider();

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IStatusService, StatusService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardView>();
        services.AddSingleton<ProfilesView>();
        services.AddSingleton<CalculationsView>();
        services.AddSingleton<RiskFlagsView>();
        services.AddSingleton<SettingsView>();
        services.AddSingleton<MainWindow>();
    }
}
