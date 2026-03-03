using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ShellViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the shell view model's core responsibilities:
/// <list type="bullet">
///   <item><description>Initialization and default state setup</description></item>
///   <item><description>Theme coordination between Material Design and custom visualizations</description></item>
///   <item><description>Unit system management and propagation to unit-aware components</description></item>
///   <item><description>Status service integration and reactive UI updates</description></item>
///   <item><description>Log management with filtering, searching, and sorting</description></item>
///   <item><description>Command execution for log viewer operations</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Mock Strategy:</strong> The tests use strict mock behavior to ensure only
/// expected interactions occur, making tests fail fast when implementation changes
/// introduce unexpected side effects.
/// </para>
/// </remarks>
public sealed class ShellViewModelTests
{
    // ============================================================
    // Test Helpers - View Model Factory
    // ============================================================

    /// <summary>
    /// Creates a <see cref="ShellViewModel"/> instance with all dependencies mocked.
    /// </summary>
    /// <param name="entries">
    /// The underlying observable collection backing the log store for direct test manipulation.
    /// </param>
    /// <param name="logStoreMock">The mocked log store.</param>
    /// <param name="statusMock">The mocked status service with reactive property change support.</param>
    /// <param name="themeMock">The mocked theme service for Material Design theming.</param>
    /// <param name="themeAwareMock">The mocked theme-aware component (typically the calculations view model).</param>
    /// <param name="unitAwareMock">The mocked unit-aware component for unit system propagation.</param>
    /// <returns>A fully initialized shell view model ready for testing.</returns>
    /// <remarks>
    /// <para>
    /// This factory method sets up all required mocks with appropriate behaviors to support
    /// the shell view model's initialization sequence:
    /// <list type="number">
    ///   <item><description>Theme service and theme-aware component are configured to accept theme changes</description></item>
    ///   <item><description>Unit-aware component is configured to accept unit system changes</description></item>
    ///   <item><description>Log store is backed by a real observable collection for realistic behavior</description></item>
    ///   <item><description>Status service supports reactive property change notifications</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// During construction, the view model:
    /// <list type="bullet">
    ///   <item><description>Sets <c>IsDarkMode = true</c> (calls theme service and theme-aware component)</description></item>
    ///   <item><description>Sets <c>UnitSystem = Standard</c> (propagates to unit-aware component)</description></item>
    ///   <item><description>Calls <c>status.Set("Ready", ...)</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static ShellViewModel CreateVm(
        out ObservableCollection<LogEntry> entries,
        out Mock<IObservableLogStore> logStoreMock,
        out Mock<IStatusService> statusMock,
        out Mock<IThemeService> themeMock,
        out Mock<IThemeAware> themeAwareMock,
        out Mock<IUnitAware> unitAwareMock)
    {
        themeMock = new Mock<IThemeService>(MockBehavior.Strict);
        themeAwareMock = new Mock<IThemeAware>(MockBehavior.Strict);
        unitAwareMock = new Mock<IUnitAware>(MockBehavior.Strict);

        // Setup mocks to support constructor initialization
        themeMock.Setup(x => x.SetDark(It.IsAny<bool>()));
        themeAwareMock.Setup(x => x.SetTheme(It.IsAny<bool>()));
        unitAwareMock.SetupSet(x => x.UnitSystem = It.IsAny<UnitSystem>());

        logStoreMock = CreateLogStoreMock(out entries);
        statusMock = CreateStatusServiceMock();

        return new ShellViewModel(
            themeMock.Object,
            statusMock.Object,
            themeAwareMock.Object,
            unitAwareMock.Object,
            logStoreMock.Object);
    }

    // ============================================================
    // Test Helpers - Mock Factories
    // ============================================================

    /// <summary>
    /// Creates a mock <see cref="IObservableLogStore"/> backed by a real observable collection.
    /// </summary>
    /// <param name="entries">
    /// Outputs the underlying observable collection for direct test manipulation.
    /// </param>
    /// <returns>A configured mock log store.</returns>
    /// <remarks>
    /// The mock uses a real <see cref="ObservableCollection{T}"/> wrapped in a
    /// <see cref="ReadOnlyObservableCollection{T}"/> to provide realistic collection
    /// change notifications while preventing modification through the read-only interface.
    /// This approach allows tests to manipulate entries directly while verifying that
    /// the view model properly reacts to collection changes.
    /// </remarks>
    private static Mock<IObservableLogStore> CreateLogStoreMock(out ObservableCollection<LogEntry> entries)
    {
        entries = new ObservableCollection<LogEntry>();
        var readOnly = new ReadOnlyObservableCollection<LogEntry>(entries);

        var mock = new Mock<IObservableLogStore>(MockBehavior.Strict);

        mock.SetupGet(x => x.Entries).Returns(readOnly);
        mock.Setup(x => x.Clear()).Callback(entries.Clear);

        return mock;
    }

    /// <summary>
    /// Creates a mock <see cref="IStatusService"/> with reactive property change support.
    /// </summary>
    /// <returns>A configured mock status service.</returns>
    /// <remarks>
    /// <para>
    /// This mock provides a realistic implementation that:
    /// <list type="bullet">
    ///   <item><description>Maintains internal state for the <see cref="IStatusService.Current"/> property</description></item>
    ///   <item><description>Raises <see cref="INotifyPropertyChanged.PropertyChanged"/> events when <see cref="IStatusService.Set"/> is called</description></item>
    ///   <item><description>Allows tests to subscribe to property change events</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="IStatusService.Current"/> property is read-only and backed by a captured variable
    /// that is updated when <see cref="IStatusService.Set"/> is called. This pattern allows tests to
    /// verify that the view model properly reacts to status changes from the service.
    /// </para>
    /// </remarks>
    private static Mock<IStatusService> CreateStatusServiceMock()
    {
        var mock = new Mock<IStatusService>(MockBehavior.Strict);

        StatusState current = new(
            AppStatusLevel.Ok,
            "Init",
            "Init");

        mock.SetupGet(x => x.Current).Returns(() => current);

        mock.SetupAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());
        mock.SetupRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>());

        mock.Setup(x => x.Set(
                It.IsAny<AppStatusLevel>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<AppStatusLevel, string, string>((lvl, left, right) =>
            {
                current = new StatusState(lvl, left, right);

                mock.Raise(
                    x => x.PropertyChanged += null!,
                    new PropertyChangedEventArgs(nameof(IStatusService.Current)));
            });

        return mock;
    }

    // ============================================================
    // Test Helpers - Test Data Factories
    // ============================================================

    /// <summary>
    /// Creates a test <see cref="LogEntry"/> with the specified parameters.
    /// </summary>
    /// <param name="ts">The timestamp for the log entry.</param>
    /// <param name="level">The log level.</param>
    /// <param name="message">The log message.</param>
    /// <param name="category">The logger category name (default: "Test").</param>
    /// <param name="ex">An optional exception associated with the log entry.</param>
    /// <returns>A configured log entry for test assertions.</returns>
    private static LogEntry MakeLog(
        DateTimeOffset ts,
        LogLevel level,
        string message,
        string category = "Test",
        Exception? ex = null)
        => new(
            Timestamp: ts,
            Level: level,
            Category: category,
            EventId: new EventId(0, null),
            Message: message,
            Exception: ex,
            Properties: Array.Empty<KeyValuePair<string, object?>>(),
            Scopes: Array.Empty<KeyValuePair<string, object?>>());

    // ============================================================
    // Tests - Constructor and Initialization
    // ============================================================

    [Fact]
    public void Constructor_SetsInitialStatus()
    {
        var vm = CreateVm(
            out _,
            out _,
            out var statusMock,
            out _,
            out _,
            out _);

        statusMock.Verify(s => s.Set(
                AppStatusLevel.Ok,
                "Ready",
                "Ready for Calculation"),
            Times.Once);

        Assert.Equal("Ready", vm.StatusLeftText);
        Assert.Equal("Ready for Calculation", vm.StatusRightText);
    }

    [Fact]
    public void Constructor_InitializesCurrentViewModel_ToThemeAware()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out var themeAwareMock,
            out _);

        Assert.Same(themeAwareMock.Object, vm.CurrentViewModel);
    }

    [Fact]
    public void Constructor_InitializesLogView_SortedNewestFirst()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-10), LogLevel.Information, "old"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "new"));

        vm.LogEntriesView.Refresh();

        var first = vm.LogEntriesView.Cast<LogEntry>().First();
        Assert.Equal("new", first.Message);
    }

    [Fact]
    public void Constructor_InitializesTheme_ToDark()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out var themeMock,
            out var themeAwareMock,
            out _);

        // Verify constructor effects (IsDarkMode = true)
        themeMock.Verify(t => t.SetDark(true), Times.Once);
        themeAwareMock.Verify(t => t.SetTheme(true), Times.Once);

        Assert.True(vm.IsDarkMode);
    }

    [Fact]
    public void Constructor_InitializesUnitSystem_ToStandard_AndPropagates()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock);

        Assert.Equal(UnitSystem.Standard, vm.UnitSystem);
        Assert.False(vm.IsAmericanStandard);

        // _unitSystem starts as Standard, so constructor's UnitSystem = Standard is a no-op
        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.Standard, Times.Never);
    }

    // ============================================================
    // Tests - Theme Coordination
    // ============================================================

    [Fact]
    public void IsDarkMode_Setter_CallsThemeService_AndThemeAware()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out var themeMock,
            out var themeAwareMock,
            out _);

        // Ignore constructor calls
        themeMock.Invocations.Clear();
        themeAwareMock.Invocations.Clear();

        vm.IsDarkMode = false;

        themeMock.Verify(t => t.SetDark(false), Times.Once);
        themeAwareMock.Verify(t => t.SetTheme(false), Times.Once);
    }

    // ============================================================
    // Tests - Unit System Management
    // ============================================================

    [Fact]
    public void SettingUnitSystem_PropagatesToUnitAware_AndNotifies()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock);

        unitAwareMock.Invocations.Clear(); // Ignore constructor propagation

        vm.UnitSystem = UnitSystem.AmericanStandard;

        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.AmericanStandard, Times.Once);

        Assert.True(vm.IsAmericanStandard);
        Assert.Equal("American", vm.UnitSystemRightText);
    }

    [Fact]
    public void SettingIsAmericanStandard_TogglesUnitSystem()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock);

        unitAwareMock.Invocations.Clear();

        vm.IsAmericanStandard = true;

        Assert.Equal(UnitSystem.AmericanStandard, vm.UnitSystem);
        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.AmericanStandard, Times.Once);

        unitAwareMock.Invocations.Clear();

        vm.IsAmericanStandard = false;

        Assert.Equal(UnitSystem.Standard, vm.UnitSystem);
        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.Standard, Times.Once);
    }

    // ============================================================
    // Tests - Status Service Integration
    // ============================================================

    [Fact]
    public void When_StatusServiceCurrentChanges_ViewModelNotifiesBindings()
    {
        var vm = CreateVm(
            out _,
            out _,
            out var statusMock,
            out _,
            out _,
            out _);

        var changed = new Collection<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
                changed.Add(e.PropertyName!);
        };

        statusMock.Object.Set(
            AppStatusLevel.Warning,
            "Heads up",
            "Something happened");

        Assert.Contains(nameof(ShellViewModel.StatusLeftText), changed);
        Assert.Contains(nameof(ShellViewModel.StatusRightText), changed);
        Assert.Contains(nameof(ShellViewModel.StatusBrush), changed);

        Assert.Equal("Heads up", vm.StatusLeftText);
        Assert.Equal("Something happened", vm.StatusRightText);
    }

    // ============================================================
    // Tests - Log Management - Counts and State
    // ============================================================

    [Fact]
    public void LogCount_AndHasLogs_TrackStoreEntries()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _,
            out _);

        Assert.Equal(0, vm.LogCount);
        Assert.False(vm.HasLogs);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hi"));

        Assert.Equal(1, vm.LogCount);
        Assert.True(vm.HasLogs);
    }

    // ============================================================
    // Tests - Log Management - Commands
    // ============================================================

    [Fact]
    public void ShowAndCloseLogsCommand_TogglesIsLogsOpen()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);

        Assert.False(vm.IsLogsOpen);

        vm.ShowLogsCommand.Execute(null);
        Assert.True(vm.IsLogsOpen);

        vm.CloseLogsCommand.Execute(null);
        Assert.False(vm.IsLogsOpen);
    }

    [Fact]
    public void ClearLogsCommand_ClearsStore_AndResetsSelection()
    {
        var vm = CreateVm(
            out var entries,
            out var storeMock,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "a"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "b"));

        vm.SelectedLogEntry = entries[0];
        Assert.NotNull(vm.SelectedLogEntry);
        Assert.True(vm.HasLogs);

        vm.ClearLogsCommand.Execute(null);

        storeMock.Verify(s => s.Clear(), Times.Once);

        Assert.Equal(0, vm.LogCount);
        Assert.False(vm.HasLogs);
        Assert.Null(vm.SelectedLogEntry);
    }

    // ============================================================
    // Tests - Log Management - Filtering by Level
    // ============================================================

    [Fact]
    public void LogFilter_ByExactLevel_Works()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "info"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err"));

        vm.SelectedLogLevelOption = vm.LogLevelOptions.First(o => o.Level == LogLevel.Error);
        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());

        var only = vm.LogEntriesView.Cast<LogEntry>().Single();
        Assert.Equal(LogLevel.Error, only.Level);
    }

    // ============================================================
    // Tests - Log Management - Filtering by Search Text
    // ============================================================

    [Fact]
    public void LogFilter_BySearchText_Works()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hello world", category: "A"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Information, "goodbye", category: "B"));

        vm.LogSearchText = "world";
        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());

        var only = vm.LogEntriesView.Cast<LogEntry>().Single();
        Assert.Contains("world", only.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogFilter_SearchMatches_Category_AndExceptionText()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hello world", category: "Alpha"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "boom", category: "Beta", ex: new InvalidOperationException("kaboom")));

        // Search by exception text
        vm.LogSearchText = "kaboom";
        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());

        var only = vm.LogEntriesView.Cast<LogEntry>().Single();
        Assert.Equal(LogLevel.Error, only.Level);
        Assert.NotNull(only.Exception);
        Assert.Contains("kaboom", only.Exception!.ToString(), StringComparison.OrdinalIgnoreCase);

        // Search by category
        vm.LogSearchText = "alpha";
        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());
        Assert.Equal("Alpha", vm.LogEntriesView.Cast<LogEntry>().Single().Category);
    }
}