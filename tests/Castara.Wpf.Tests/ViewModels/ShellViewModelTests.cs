using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.Telemetry.Logging;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Contains unit tests for <see cref="ShellViewModel"/> to verify shell coordination,
/// log management, status service integration, theme synchronization, and filtering behavior.
/// </summary>
/// <remarks>
/// <para>
/// This test suite validates the ShellViewModel through comprehensive behavioral testing:
/// </para>
/// <para>
/// <strong>Initialization Tests:</strong> Verify constructor behavior, initial status, and log view setup.
/// </para>
/// <para>
/// <strong>Status Integration Tests:</strong> Ensure status changes from the service propagate
/// to the view model's status properties through property change notifications.
/// </para>
/// <para>
/// <strong>Log Management Tests:</strong> Validate log count tracking, command behavior (show, close, clear),
/// and collection change handling.
/// </para>
/// <para>
/// <strong>Filtering Tests:</strong> Verify log filtering by exact level and search text with
/// case-insensitive matching across message, category, and exception.
/// </para>
/// <para>
/// <strong>Theme Coordination Tests:</strong> Ensure theme changes are properly coordinated across
/// both the theme service (Material Design) and theme-aware components (OxyPlot charts).
/// </para>
/// <para>
/// The tests use Moq for dependency isolation and custom factory methods to create
/// consistent test fixtures with properly configured mocks.
/// </para>
/// </remarks>
public sealed class ShellViewModelTests
{
    // ============================================================
    // VM Factory
    // ============================================================

    /// <summary>
    /// Creates a <see cref="ShellViewModel"/> instance with mocked dependencies for testing.
    /// </summary>
    /// <param name="entries">
    /// Outputs the backing observable collection for log entries, allowing test manipulation.
    /// </param>
    /// <param name="logStoreMock">
    /// Outputs the mocked <see cref="IObservableLogStore"/> for verification.
    /// </param>
    /// <param name="statusMock">
    /// Outputs the mocked <see cref="IStatusService"/> for verification.
    /// </param>
    /// <param name="themeMock">
    /// Outputs the mocked <see cref="IThemeService"/> for verification.
    /// </param>
    /// <param name="themeAwareMock">
    /// Outputs the mocked <see cref="IThemeAware"/> (calculations view model) for verification.
    /// </param>
    /// <returns>
    /// A <see cref="ShellViewModel"/> instance with all dependencies mocked and ready for testing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This factory method sets up the required mocks with strict behavior to catch unexpected
    /// interactions. It pre-configures expected calls during construction:
    /// <list type="bullet">
    ///   <item><description><see cref="IThemeService.SetDark"/> for initial theme setup</description></item>
    ///   <item><description><see cref="IThemeAware.SetTheme"/> for chart theme coordination</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The output parameters provide direct access to mock objects and test data for assertions
    /// and verification in individual tests.
    /// </para>
    /// </remarks>
    private static ShellViewModel CreateVm(
        out ObservableCollection<LogEntry> entries,
        out Mock<IObservableLogStore> logStoreMock,
        out Mock<IStatusService> statusMock,
        out Mock<IThemeService> themeMock,
        out Mock<IThemeAware> themeAwareMock)
    {
        themeMock = new Mock<IThemeService>(MockBehavior.Strict);
        themeAwareMock = new Mock<IThemeAware>(MockBehavior.Strict);

        // ShellViewModel sets IsDarkMode=true during construction
        themeMock.Setup(x => x.SetDark(It.IsAny<bool>()));
        themeAwareMock.Setup(x => x.SetTheme(It.IsAny<bool>()));

        logStoreMock = CreateLogStoreMock(out entries);
        statusMock = CreateStatusServiceMock();

        return new ShellViewModel(
            themeMock.Object,
            statusMock.Object,
            themeAwareMock.Object,
            logStoreMock.Object);
    }

    // ============================================================
    // Log Store Mock (Moq)
    // ============================================================

    /// <summary>
    /// Creates a mock <see cref="IObservableLogStore"/> with a backing observable collection for testing.
    /// </summary>
    /// <param name="entries">
    /// Outputs the backing <see cref="ObservableCollection{T}"/> that tests can manipulate directly.
    /// </param>
    /// <returns>
    /// A <see cref="Mock{T}"/> of <see cref="IObservableLogStore"/> configured with strict behavior.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The mock is configured to:
    /// <list type="bullet">
    ///   <item><description>Return a <see cref="ReadOnlyObservableCollection{T}"/> wrapping the backing collection</description></item>
    ///   <item><description>Clear the backing collection when <see cref="IObservableLogStore.Clear"/> is called</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach allows tests to add/remove entries directly through the output parameter
    /// while the view model sees them through the read-only wrapper, simulating real usage.
    /// </para>
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

    // ============================================================
    // Status Service Mock (Moq) - Current is READ-ONLY, backed by field
    // ============================================================

    /// <summary>
    /// Creates a mock <see cref="IStatusService"/> with reactive property change notification support.
    /// </summary>
    /// <returns>
    /// A <see cref="Mock{T}"/> of <see cref="IStatusService"/> configured to simulate property changes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Since <see cref="IStatusService.Current"/> is read-only, this mock uses a backing field
    /// to store the current status state. When <see cref="IStatusService.Set"/> is called:
    /// <list type="number">
    ///   <item><description>The backing field is updated with the new status</description></item>
    ///   <item><description><see cref="INotifyPropertyChanged.PropertyChanged"/> is raised for "Current"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This simulates the real service's behavior, allowing tests to verify that the view model
    /// responds correctly to status changes through property change notifications.
    /// </para>
    /// <para>
    /// The mock supports add/remove for <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// event subscription, which the view model uses to react to status updates.
    /// </para>
    /// </remarks>
    private static Mock<IStatusService> CreateStatusServiceMock()
    {
        var mock = new Mock<IStatusService>(MockBehavior.Strict);

        // Backing field that Current returns (Current is read-only in the interface)
        StatusState current = new(
            AppStatusLevel.Ok,
            "Init",
            "Init");

        mock.SetupGet(x => x.Current).Returns(() => current);

        // ShellViewModel subscribes to PropertyChanged
        mock.SetupAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());
        mock.SetupRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>());

        // When Set is called, update backing field and raise PropertyChanged(Current)
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
    // Tests: Constructor / Initialization
    // ============================================================

    /// <summary>
    /// Verifies that the constructor sets the initial status to "Ready" with appropriate message.
    /// </summary>
    /// <remarks>
    /// This test ensures the shell view model starts in a ready state, informing users that
    /// the application is prepared for calculations. The status properties should reflect
    /// the initial status set during construction.
    /// </remarks>
    [Fact]
    public void Constructor_SetsInitialStatus()
    {
        var vm = CreateVm(
            out _,
            out _,
            out var statusMock,
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

    /// <summary>
    /// Verifies that the constructor initializes the log entries collection view with
    /// descending timestamp sort (newest entries first).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the sort configuration by:
    /// <list type="number">
    ///   <item><description>Adding two log entries with different timestamps</description></item>
    ///   <item><description>Refreshing the view to apply sorting</description></item>
    ///   <item><description>Verifying the newest entry appears first</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Newest-first ordering is important for log viewing, as users typically want to see
    /// the most recent events at the top of the list.
    /// </para>
    /// </remarks>
    [Fact]
    public void Constructor_InitializesLogView_SortedNewestFirst()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-10), LogLevel.Information, "old"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "new"));

        vm.LogEntriesView.Refresh();

        // ICollectionView doesn't have GetItemAt; enumerate to check order
        var first = vm.LogEntriesView.Cast<LogEntry>().First();
        Assert.Equal("new", first.Message);
    }

    // ============================================================
    // Tests: Status Change Propagation
    // ============================================================

    /// <summary>
    /// Verifies that status changes from the status service propagate to the view model's
    /// status properties through property change notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the reactive binding chain:
    /// <list type="number">
    ///   <item><description>Status service raises PropertyChanged for "Current"</description></item>
    ///   <item><description>View model receives notification and raises PropertyChanged for status properties</description></item>
    ///   <item><description>UI bindings update to reflect new status values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The test verifies that all three status properties (StatusLeftText, StatusRightText, StatusBrush)
    /// notify of changes and return updated values from the service.
    /// </para>
    /// </remarks>
    [Fact]
    public void When_StatusServiceCurrentChanges_ViewModelNotifiesBindings()
    {
        var vm = CreateVm(
            out _,
            out _,
            out var statusMock,
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
    // Tests: Log Counts + Commands
    // ============================================================

    /// <summary>
    /// Verifies that <see cref="ShellViewModel.LogCount"/> and <see cref="ShellViewModel.HasLogs"/>
    /// accurately track the number of entries in the log store.
    /// </summary>
    /// <remarks>
    /// These properties are used for UI display and conditional visibility of log-related
    /// controls. The test verifies they update correctly as entries are added to the store.
    /// </remarks>
    [Fact]
    public void LogCount_AndHasLogs_TrackStoreEntries()
    {
        var vm = CreateVm(
            out var entries,
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

    /// <summary>
    /// Verifies that <see cref="ShellViewModel.ShowLogsCommand"/> and
    /// <see cref="ShellViewModel.CloseLogsCommand"/> correctly toggle the
    /// <see cref="ShellViewModel.IsLogsOpen"/> property.
    /// </summary>
    /// <remarks>
    /// These commands control the visibility of the log viewer dialog. The test ensures
    /// they properly open and close the dialog through property changes.
    /// </remarks>
    [Fact]
    public void ShowAndCloseLogsCommand_TogglesIsLogsOpen()
    {
        var vm = CreateVm(
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

    /// <summary>
    /// Verifies that <see cref="ShellViewModel.ClearLogsCommand"/> clears the log store,
    /// resets the selected entry, and updates count properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the complete clear workflow:
    /// <list type="number">
    ///   <item><description>Calls <see cref="IObservableLogStore.Clear"/> on the log store</description></item>
    ///   <item><description>Clears the selected log entry</description></item>
    ///   <item><description>Updates LogCount and HasLogs properties</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Clearing logs is important for managing memory usage and resetting the diagnostic
    /// state during long-running sessions.
    /// </para>
    /// </remarks>
    [Fact]
    public void ClearLogsCommand_ClearsStore_AndResetsSelection()
    {
        var vm = CreateVm(
            out var entries,
            out var storeMock,
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
    // Tests: Filtering
    // ============================================================

    /// <summary>
    /// Verifies that log filtering by exact level works correctly, showing only entries
    /// matching the selected log level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates exact level matching (not minimum level filtering):
    /// <list type="bullet">
    ///   <item><description>Information entries are hidden when Error is selected</description></item>
    ///   <item><description>Only Error entries are visible</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Exact level filtering allows users to focus on specific log levels without
    /// seeing higher or lower severity entries.
    /// </para>
    /// </remarks>
    [Fact]
    public void LogFilter_ByExactLevel_Works()
    {
        var vm = CreateVm(
            out var entries,
            out _,
            out _,
            out _,
            out _);

        entries.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "info"));
        entries.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err"));

        // Set filter option to Error (exact match)
        vm.SelectedLogLevelOption = vm.LogLevelOptions.First(o => o.Level == LogLevel.Error);

        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());

        var only = vm.LogEntriesView.Cast<LogEntry>().Single();
        Assert.Equal(LogLevel.Error, only.Level);
    }

    /// <summary>
    /// Verifies that log filtering by search text works correctly with case-insensitive matching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The search filter performs case-insensitive substring matching across:
    /// <list type="bullet">
    ///   <item><description>Log message text</description></item>
    ///   <item><description>Category names</description></item>
    ///   <item><description>Exception text (if present)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This test verifies that only entries containing the search term are visible
    /// after filtering is applied.
    /// </para>
    /// </remarks>
    [Fact]
    public void LogFilter_BySearchText_Works()
    {
        var vm = CreateVm(
            out var entries,
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

    // ============================================================
    // Tests: Theming Coordination
    // ============================================================

    /// <summary>
    /// Verifies that setting <see cref="ShellViewModel.IsDarkMode"/> coordinates theme changes
    /// across both the theme service (Material Design) and theme-aware components (OxyPlot charts).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shell view model acts as the single coordination point for theme changes,
    /// ensuring both systems update together:
    /// <list type="bullet">
    ///   <item><description><see cref="IThemeService.SetDark"/> updates Material Design UI components</description></item>
    ///   <item><description><see cref="IThemeAware.SetTheme"/> updates OxyPlot chart colors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This test verifies both calls occur when the IsDarkMode property changes, maintaining
    /// consistent theming across the entire application.
    /// </para>
    /// </remarks>
    [Fact]
    public void IsDarkMode_Setter_CallsThemeService_AndThemeAware()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out var themeMock,
            out var themeAwareMock);

        // Clear constructor invocations to test only the property setter
        themeMock.Invocations.Clear();
        themeAwareMock.Invocations.Clear();

        vm.IsDarkMode = false;

        themeMock.Verify(t => t.SetDark(false), Times.Once);
        themeAwareMock.Verify(t => t.SetTheme(false), Times.Once);
    }

    // ============================================================
    // Test Data Helpers
    // ============================================================

    /// <summary>
    /// Creates a test <see cref="LogEntry"/> with specified parameters.
    /// </summary>
    /// <param name="ts">The timestamp for the log entry.</param>
    /// <param name="level">The log level (severity).</param>
    /// <param name="message">The log message text.</param>
    /// <param name="category">The logger category name (default: "Test").</param>
    /// <param name="ex">An optional exception associated with the log entry.</param>
    /// <returns>
    /// A <see cref="LogEntry"/> instance configured with the specified parameters.
    /// </returns>
    /// <remarks>
    /// This helper simplifies test data creation by providing sensible defaults and
    /// a fluent interface for creating log entries with only the parameters relevant
    /// to each test.
    /// </remarks>
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