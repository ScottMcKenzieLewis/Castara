using Castara.Application.Abstractions.Repositories;
using Castara.Domain.Estimation.Services;
using Castara.Wpf.Diagnostics.CrashReport.Interfaces;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Clipboard;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace Castara.Wpf.Tests.Common;

/// <summary>
/// Shared test context for WPF view model tests.
/// Centralizes mocks and factory methods so test files stay focused on behavior.
/// </summary>
public sealed class TestContext
{
    /// <summary>
    /// Mock for the status service that manages application status messages and levels.
    /// </summary>
    public Mock<IStatusService> Status { get; } = new();

    /// <summary>
    /// Mock for the cast iron estimator service that performs metallurgical calculations.
    /// </summary>
    public Mock<ICastIronEstimator> Estimator { get; } = new();

    /// <summary>
    /// Backing collection for log entries. Use this to add or remove entries in tests.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntriesBacking { get; } = new();

    /// <summary>
    /// Read-only observable collection of log entries exposed to view models.
    /// Wraps <see cref="LogEntriesBacking"/>.
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// Mock for the observable log store that provides access to application logs.
    /// </summary>
    public Mock<IObservableLogStore> LogStore { get; } = new();

    /// <summary>
    /// Mock for the clipboard service that handles copying text to the system clipboard.
    /// </summary>
    public Mock<IClipboardService> Clipboard { get; } = new();

    /// <summary>
    /// Mock for the theme service that manages application theme switching.
    /// </summary>
    public Mock<IThemeService> ThemeService { get; } = new();

    /// <summary>
    /// Mock for theme-aware components that respond to theme changes.
    /// </summary>
    public Mock<IThemeAware> ThemeAware { get; } = new();

    /// <summary>
    /// Mock for unit-aware components that respond to unit system changes.
    /// </summary>
    public Mock<IUnitAware> UnitAware { get; } = new();

    /// <summary>
    /// Mock for casting profile-aware components that respond to profile changes.
    /// </summary>
    public Mock<ICastingProfileAware> CastingProfileAware { get; } = new();

    /// <summary>
    /// Mock for the casting profile repository that manages profile persistence.
    /// </summary>
    public Mock<ICastingProfileRepository> CastingProfileRepository { get; } = new();

    /// <summary>
    /// Mock for the application state snapshot service used in crash reporting.
    /// </summary>
    public Mock<IApplicationStateSnapshotService> ApplicationStateSnapshotService { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class with pre-configured mocks.
    /// </summary>
    /// <remarks>
    /// Sets up default behaviors for common scenarios:
    /// <list type="bullet">
    /// <item><description>Log store returns the shared log entries collection</description></item>
    /// <item><description>Clipboard accepts any text</description></item>
    /// <item><description>Status service returns a default "Ready" state</description></item>
    /// </list>
    /// </remarks>
    public TestContext()
    {
        LogEntries = new ReadOnlyObservableCollection<LogEntry>(LogEntriesBacking);

        LogStore.SetupGet(x => x.Entries).Returns(LogEntries);
        LogStore.Setup(x => x.Clear()).Callback(LogEntriesBacking.Clear);

        Clipboard.Setup(x => x.SetText(It.IsAny<string>()));

        Status.SetupGet(x => x.Current)
            .Returns(new StatusState(AppStatusLevel.Ok, "Ready", "Select a casting profile"));
    }

    /// <summary>
    /// Creates a CalculationsViewModel using the shared mocks in this context.
    /// </summary>
    public CalculationsViewModel CreateCalculationsViewModel()
    {
        return new CalculationsViewModel(
            Status.Object,
            Estimator.Object,
            ApplicationStateSnapshotService.Object,
            NullLogger<CalculationsViewModel>.Instance);
    }

    /// <summary>
    /// Creates a LogViewerViewModel using the shared mocks in this context.
    /// </summary>
    public LogViewerViewModel CreateLogViewerViewModel()
    {
        return new LogViewerViewModel(
            LogStore.Object,
            Clipboard.Object);
    }

    /// <summary>
    /// Creates a ShellViewModel using the shared mocks in this context.
    /// Adjust constructor ordering/types if your ShellViewModel differs.
    /// </summary>
    public ShellViewModel CreateShellViewModel()
    {
        return new ShellViewModel(
            ThemeService.Object,
            Status.Object,
            ThemeAware.Object,
            UnitAware.Object,
            CastingProfileAware.Object,
            CastingProfileRepository.Object,
            ApplicationStateSnapshotService.Object,
            CreateLogViewerViewModel(),
            NullLogger<ShellViewModel>.Instance);
    }
}