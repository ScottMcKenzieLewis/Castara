using Castara.Domain.Estimation.Services;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Castara.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Castara.Wpf;

/// <summary>
/// The main application class for Castara, responsible for dependency injection configuration,
/// service registration, and application lifecycle management.
/// </summary>
/// <remarks>
/// This application uses Microsoft.Extensions.DependencyInjection for dependency injection,
/// providing a clean separation of concerns and testability. Services are configured as
/// singletons to maintain state across the application lifetime.
/// </remarks>
public partial class App : System.Windows.Application
{
    /// <summary>
    /// The service provider containing all registered application services.
    /// </summary>
    /// <remarks>
    /// This field is initialized during application startup and provides access to
    /// dependency injection services throughout the application lifecycle.
    /// </remarks>
    private ServiceProvider? _services;

    /// <summary>
    /// Handles the application startup event, configuring dependency injection and
    /// displaying the main window.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    /// <remarks>
    /// <para>
    /// The startup process follows these steps:
    /// <list type="number">
    ///   <item><description>Create a service collection for dependency registration</description></item>
    ///   <item><description>Configure all application services (domain, infrastructure, presentation)</description></item>
    ///   <item><description>Build the service provider</description></item>
    ///   <item><description>Resolve and display the main window</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach ensures all dependencies are properly initialized before any
    /// application components are created.
    /// </para>
    /// </remarks>
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _services = services.BuildServiceProvider();

        var shellVm = _services.GetRequiredService<ShellViewModel>();
        var window = _services.GetRequiredService<MainWindow>();
        window.DataContext = shellVm;
        window.Show();
    }

    /// <summary>
    /// Configures all application services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <remarks>
    /// <para>
    /// Services are registered with the following lifetimes:
    /// </para>
    /// <para>
    /// <strong>Domain Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="ICastIronEstimator"/> - Core estimation engine for cast iron properties</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Infrastructure Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="IStatusService"/> - Application-wide status and notification management</description></item>
    ///   <item><description><see cref="IThemeService"/> - Material Design theme switching and customization</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Presentation Services:</strong>
    /// <list type="bullet">
    ///   <item><description><see cref="MainWindow"/> - Main application window</description></item>
    ///   <item><description><see cref="ShellViewModel"/> - Shell navigation and application state</description></item>
    ///   <item><description><see cref="CalculationsView"/> - Calculations view component</description></item>
    ///   <item><description><see cref="CalculationsViewModel"/> - Calculations view model with business logic</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All services are registered as singletons to maintain state across the application
    /// and ensure efficient resource usage.
    /// </para>
    /// </remarks>
    private void ConfigureServices(IServiceCollection services)
    {
        // Domain Services
        services.AddSingleton<ICastIronEstimator, CastIronEstimator>();

        // Infrastructure Services
        services.AddSingleton<IStatusService, StatusService>();
        services.AddSingleton<IThemeService, ThemeService>();


        // Presentation Services
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<CalculationsViewModel>(); // or AddTransient; either is fine
        services.AddSingleton<IThemeAware>(sp => sp.GetRequiredService<CalculationsViewModel>());

    }
}
