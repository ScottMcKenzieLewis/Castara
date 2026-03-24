using Castara.Application.Abstractions.Repositories;
using Castara.Domain.Estimation.Services;
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
    public Mock<IStatusService> Status { get; } = new();
    public Mock<ICastIronEstimator> Estimator { get; } = new();

    public ObservableCollection<LogEntry> LogEntriesBacking { get; } = new();
    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }

    public Mock<IObservableLogStore> LogStore { get; } = new();
    public Mock<IClipboardService> Clipboard { get; } = new();

    public Mock<IThemeService> ThemeService { get; } = new();
    public Mock<IThemeAware> ThemeAware { get; } = new();
    public Mock<IUnitAware> UnitAware { get; } = new();
    public Mock<ICastingProfileAware> CastingProfileAware { get; } = new();
    public Mock<ICastingProfileRepository> CastingProfileRepository { get; } = new();

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
            CreateLogViewerViewModel(),
            NullLogger<ShellViewModel>.Instance);
    }
}