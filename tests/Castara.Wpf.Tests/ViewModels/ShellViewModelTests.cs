using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Castara.Wpf.Infrastructure.Abstractions;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Clipboard;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Contains unit tests for <see cref="ShellViewModel"/> to verify shell coordination,
/// theme management, unit system propagation, status service integration, and child view model management.
/// </summary>
/// <remarks>
/// <para>
/// This test suite validates the ShellViewModel's role as the application-level coordinator through comprehensive behavioral testing:
/// </para>
/// <para>
/// <strong>Initialization Tests:</strong> Verify constructor behavior, default values (dark theme, Standard units),
/// initial status setting, and view model hosting.
/// </para>
/// <para>
/// <strong>Theme Coordination Tests:</strong> Ensure theme changes propagate to both the theme service
/// (Material Design UI) and theme-aware components (OxyPlot charts) through a single synchronization point.
/// </para>
/// <para>
/// <strong>Unit System Coordination Tests:</strong> Validate that unit system changes propagate to
/// unit-aware components (CalculationsViewModel) and update related UI text properties.
/// </para>
/// <para>
/// <strong>Status Integration Tests:</strong> Ensure status changes from the service propagate
/// to the view model's status properties through reactive property change notifications.
/// </para>
/// <para>
/// <strong>Child View Model Tests:</strong> Verify proper exposure and management of the
/// LogViewerViewModel child view model.
/// </para>
/// <para>
/// The tests use Moq for dependency isolation with strict behavior to catch unexpected interactions.
/// All expected constructor calls (theme init, unit init) are pre-configured in the factory methods.
/// </para>
/// </remarks>
public sealed class ShellViewModelTests
{
    // ============================================================
    // Factory Methods
    // ============================================================

    /// <summary>
    /// Creates a <see cref="ShellViewModel"/> instance with mocked dependencies for testing.
    /// </summary>
    /// <param name="themeMock">
    /// Outputs the mocked <see cref="IThemeService"/> for Material Design theme switching verification.
    /// </param>
    /// <param name="statusMock">
    /// Outputs the mocked <see cref="IStatusService"/> with reactive property change support.
    /// </param>
    /// <param name="themeAwareMock">
    /// Outputs the mocked <see cref="IThemeAware"/> (typically CalculationsViewModel) for chart theme coordination verification.
    /// </param>
    /// <param name="clipboardMock">
    /// Outputs the mocked <see cref="IClipboardService"/> for clipboard operation verification in LogViewerViewModel.
    /// </param>
    /// <param name="unitAwareMock">
    /// Outputs the mocked <see cref="IUnitAware"/> (typically CalculationsViewModel) for unit system coordination verification.
    /// </param>
    /// <param name="logStoreMock">
    /// Outputs the mocked <see cref="IObservableLogStore"/> for log storage verification.
    /// </param>
    /// <param name="logEntries">
    /// Outputs the backing observable collection for log entries, allowing test manipulation.
    /// </param>
    /// <param name="logViewerVm">
    /// Outputs the real <see cref="LogViewerViewModel"/> instance (with mocked store and clipboard) for child VM verification.
    /// </param>
    /// <returns>
    /// A <see cref="ShellViewModel"/> instance with all dependencies mocked and ready for testing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This factory method sets up the required mocks with strict behavior to catch unexpected
    /// interactions. It pre-configures expected calls during construction:
    /// <list type="bullet">
    ///   <item><description><see cref="IThemeService.SetDark"/> with true for initial dark mode</description></item>
    ///   <item><description><see cref="IThemeAware.SetTheme"/> with true for chart theme coordination</description></item>
    ///   <item><description><see cref="IUnitAware.UnitSystem"/> set to Standard for initial unit system</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The LogViewerViewModel is a real instance to test child view model management, but its
    /// dependencies (log store and clipboard service) are mocked for isolation.
    /// </para>
    /// <para>
    /// The output parameters provide direct access to mock objects and test data for assertions
    /// and verification in individual tests.
    /// </para>
    /// </remarks>
    private static ShellViewModel CreateVm(
        out Mock<IThemeService> themeMock,
        out Mock<IStatusService> statusMock,
        out Mock<IThemeAware> themeAwareMock,
        out Mock<IClipboardService> clipboardMock,
        out Mock<IUnitAware> unitAwareMock,
        out Mock<IObservableLogStore> logStoreMock,
        out ObservableCollection<LogEntry> logEntries,
        out LogViewerViewModel logViewerVm)
    {
        themeMock = new Mock<IThemeService>(MockBehavior.Strict);
        themeAwareMock = new Mock<IThemeAware>(MockBehavior.Strict);
        unitAwareMock = new Mock<IUnitAware>(MockBehavior.Strict);
        clipboardMock = new Mock<IClipboardService>(MockBehavior.Strict);

        // Theme init happens in ctor via IsDarkMode = true
        themeMock.Setup(t => t.SetDark(true));
        themeAwareMock.Setup(t => t.SetTheme(true));

        // Unit init happens in ctor via UnitSystem = Standard
        unitAwareMock.SetupSet(u => u.UnitSystem = UnitSystem.Standard);

        // Status service: Current is read-only, but Shell subscribes to PropertyChanged
        statusMock = CreateStatusServiceMock();

        // LogViewer VM is a real instance, but its store and clipboard are mocked
        logStoreMock = CreateLogStoreMock(out logEntries);
        logViewerVm = new LogViewerViewModel(logStoreMock.Object, clipboardMock.Object);

        return new ShellViewModel(
            themeMock.Object,
            statusMock.Object,
            themeAwareMock.Object,
            unitAwareMock.Object,
            logViewerVm);
    }

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
    /// while the LogViewerViewModel sees them through the read-only wrapper, simulating real usage.
    /// </para>
    /// </remarks>
    private static Mock<IObservableLogStore> CreateLogStoreMock(out ObservableCollection<LogEntry> entries)
    {
        entries = new ObservableCollection<LogEntry>();
        var readOnly = new ReadOnlyObservableCollection<LogEntry>(entries);

        var mock = new Mock<IObservableLogStore>(MockBehavior.Strict);
        mock.SetupGet(s => s.Entries).Returns(readOnly);
        mock.Setup(s => s.Clear()).Callback(entries.Clear);

        return mock;
    }

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

        StatusState current = new(
            AppStatusLevel.Ok,
            "Init",
            "Init");

        mock.SetupGet(s => s.Current).Returns(() => current);

        // Shell subscribes to PropertyChanged
        mock.SetupAdd(s => s.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());
        mock.SetupRemove(s => s.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>());

        // When Set is called: update backing field + raise PropertyChanged("Current")
        mock.Setup(s => s.Set(It.IsAny<AppStatusLevel>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<AppStatusLevel, string, string>((lvl, left, right) =>
            {
                current = new StatusState(lvl, left, right);

                mock.Raise(
                    s => s.PropertyChanged += null!,
                    new PropertyChangedEventArgs(nameof(IStatusService.Current)));
            });

        return mock;
    }

    // ============================================================
    // Tests: Constructor / Initialization
    // ============================================================

    /// <summary>
    /// Verifies that the constructor sets the current view model to the theme-aware component
    /// (typically CalculationsViewModel).
    /// </summary>
    /// <remarks>
    /// The ShellViewModel hosts the main content view through the CurrentViewModel property.
    /// This test ensures proper initialization of the view hosting mechanism, which is critical
    /// for displaying the calculations interface.
    /// </remarks>
    [Fact]
    public void Constructor_SetsCurrentViewModel_ToThemeAware()
    {
        var vm = CreateVm(
            out _,
            out _,
            out var themeAwareMock,
            out _,
            out _,
            out _,
            out _,
            out _);

        Assert.Same(themeAwareMock.Object, vm.CurrentViewModel);
    }

    /// <summary>
    /// Verifies that the constructor initializes the unit system to Standard (SI) and
    /// propagates this value to unit-aware components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates four aspects of unit system initialization:
    /// <list type="number">
    ///   <item><description>The UnitSystem property is set to Standard (SI units: mm, °C/s)</description></item>
    ///   <item><description>The IsAmericanStandard convenience property reflects this (false)</description></item>
    ///   <item><description>UI text properties display appropriate values ("Units", "Standard")</description></item>
    ///   <item><description>The value is propagated to IUnitAware components (CalculationsViewModel)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Standard units are the default as they match the canonical storage format, requiring
    /// no conversion for calculations.
    /// </para>
    /// </remarks>
    [Fact]
    public void Constructor_InitializesUnitSystem_ToStandard_AndPropagates()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock,
            out _,
            out _,
            out _);

        Assert.Equal(UnitSystem.Standard, vm.UnitSystem);
        Assert.False(vm.IsAmericanStandard);
        Assert.Equal("Units", vm.UnitSystemLeftText);
        Assert.Equal("Standard", vm.UnitSystemRightText);

        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.Standard, Times.Once);
    }

    /// <summary>
    /// Verifies that the constructor initializes the theme to dark mode and propagates
    /// this setting to both the theme service and theme-aware components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the theme initialization workflow:
    /// <list type="number">
    ///   <item><description>IsDarkMode property is set to true</description></item>
    ///   <item><description>Theme service is called to update Material Design components</description></item>
    ///   <item><description>Theme-aware components are notified to update chart colors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Dark mode is the default theme for the application. The single synchronization point
    /// ensures consistent theming across all UI elements.
    /// </para>
    /// </remarks>
    [Fact]
    public void Constructor_InitializesTheme_ToDark_AndPropagates()
    {
        var vm = CreateVm(
            out var themeMock,
            out _,
            out var themeAwareMock,
            out _,
            out _,
            out _,
            out _,
            out _);

        Assert.True(vm.IsDarkMode);

        themeMock.Verify(t => t.SetDark(true), Times.Once);
        themeAwareMock.Verify(t => t.SetTheme(true), Times.Once);
    }

    /// <summary>
    /// Verifies that the constructor sets the initial application status to "Ready for Calculation".
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the initial status display:
    /// <list type="bullet">
    ///   <item><description>Status service Set method is called with "Ready" and "Ready for Calculation"</description></item>
    ///   <item><description>StatusLeftText property returns "Ready"</description></item>
    ///   <item><description>StatusRightText property returns "Ready for Calculation"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The initial "Ready" status informs users that the application is prepared to accept
    /// input and perform calculations.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> The status mock's Current property starts as "Init" but is updated when
    /// the constructor calls Set, triggering the callback that updates the backing field.
    /// </para>
    /// </remarks>
    [Fact]
    public void Constructor_SetsInitialStatus()
    {
        var vm = CreateVm(
            out _,
            out var statusMock,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);

        statusMock.Verify(s => s.Set(AppStatusLevel.Ok, "Ready", "Ready for Calculation"), Times.Once);

        Assert.Equal("Ready", vm.StatusLeftText);
        Assert.Equal("Ready for Calculation", vm.StatusRightText);
    }

    // ============================================================
    // Tests: Theme Coordination
    // ============================================================

    /// <summary>
    /// Verifies that setting the IsDarkMode property propagates theme changes to both
    /// the theme service (Material Design) and theme-aware components (OxyPlot charts).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the theme coordination workflow:
    /// <list type="number">
    ///   <item><description>Previous constructor invocations are cleared to isolate this test</description></item>
    ///   <item><description>IsDarkMode property is set to false (light mode)</description></item>
    ///   <item><description>Theme service SetDark is called with false</description></item>
    ///   <item><description>Theme-aware component SetTheme is called with false</description></item>
    ///   <item><description>Property getter reflects the new value</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The shell provides a single synchronization point for theme changes, ensuring
    /// both Material Design UI components and custom OxyPlot charts update consistently.
    /// This prevents visual inconsistencies where UI and charts might use different themes.
    /// </para>
    /// </remarks>
    [Fact]
    public void IsDarkMode_Setter_CallsThemeService_AndThemeAware()
    {
        var vm = CreateVm(
            out var themeMock,
            out _,
            out var themeAwareMock,
            out _,
            out _,
            out _,
            out _,
            out _);

        // Ignore ctor calls; verify only this change
        themeMock.Invocations.Clear();
        themeAwareMock.Invocations.Clear();

        themeMock.Setup(t => t.SetDark(false));
        themeAwareMock.Setup(t => t.SetTheme(false));

        vm.IsDarkMode = false;

        themeMock.Verify(t => t.SetDark(false), Times.Once);
        themeAwareMock.Verify(t => t.SetTheme(false), Times.Once);
        Assert.False(vm.IsDarkMode);
    }

    // ============================================================
    // Tests: Unit System Coordination
    // ============================================================

    /// <summary>
    /// Verifies that changing the unit system propagates to unit-aware components and
    /// updates related UI text properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the unit system coordination workflow:
    /// <list type="number">
    ///   <item><description>Previous constructor invocations are cleared to isolate this test</description></item>
    ///   <item><description>UnitSystem property is changed to AmericanStandard</description></item>
    ///   <item><description>Unit-aware component receives the new value</description></item>
    ///   <item><description>IsAmericanStandard convenience property updates to true</description></item>
    ///   <item><description>UnitSystemRightText updates to "American"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The shell coordinates unit system changes to ensure all components use consistent
    /// display units (in/°F/s or mm/°C/s) while maintaining canonical SI storage for calculations.
    /// </para>
    /// </remarks>
    [Fact]
    public void UnitSystem_WhenChanged_PropagatesToUnitAware_AndUpdatesText()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock,
            out _,
            out _,
            out _);

        // ctor propagated Standard once; now verify change
        unitAwareMock.Invocations.Clear();

        unitAwareMock.SetupSet(u => u.UnitSystem = UnitSystem.AmericanStandard);

        vm.UnitSystem = UnitSystem.AmericanStandard;

        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.AmericanStandard, Times.Once);
        Assert.True(vm.IsAmericanStandard);
        Assert.Equal("American", vm.UnitSystemRightText);
    }

    /// <summary>
    /// Verifies that toggling the IsAmericanStandard convenience property updates the
    /// underlying UnitSystem enum value and propagates to unit-aware components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the boolean wrapper pattern:
    /// <list type="number">
    ///   <item><description>Previous constructor invocations are cleared to isolate this test</description></item>
    ///   <item><description>IsAmericanStandard is set to true</description></item>
    ///   <item><description>UnitSystem property updates to AmericanStandard</description></item>
    ///   <item><description>Change propagates to IUnitAware components</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The IsAmericanStandard property provides a boolean convenience wrapper around the
    /// UnitSystem enum for easier binding to toggle controls in XAML (ToggleButton.IsChecked).
    /// </para>
    /// </remarks>
    [Fact]
    public void IsAmericanStandard_Toggle_UpdatesUnitSystem()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out var unitAwareMock,
            out _,
            out _,
            out _);

        unitAwareMock.Invocations.Clear();
        unitAwareMock.SetupSet(u => u.UnitSystem = UnitSystem.AmericanStandard);

        vm.IsAmericanStandard = true;

        unitAwareMock.VerifySet(u => u.UnitSystem = UnitSystem.AmericanStandard, Times.Once);
        Assert.Equal(UnitSystem.AmericanStandard, vm.UnitSystem);
    }

    // ============================================================
    // Tests: Status Propagation
    // ============================================================

    /// <summary>
    /// Verifies that status changes from the status service propagate to the view model's
    /// status properties through reactive property change notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the reactive binding chain:
    /// <list type="number">
    ///   <item><description>Status service Set method is called to change status to Warning</description></item>
    ///   <item><description>Service's backing field is updated and PropertyChanged("Current") is raised</description></item>
    ///   <item><description>View model receives notification and raises PropertyChanged for three status properties</description></item>
    ///   <item><description>Status text properties return updated values from the service</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The shell subscribes to status service changes in its constructor, enabling reactive
    /// status updates throughout the application lifecycle without manual refresh. This ensures
    /// the UI always displays current status information.
    /// </para>
    /// </remarks>
    [Fact]
    public void When_StatusServiceCurrentChanges_ViewModelNotifiesBindings()
    {
        var vm = CreateVm(
            out _,
            out var statusMock,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);

        var changed = new ObservableCollection<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
                changed.Add(e.PropertyName!);
        };

        // Use the mocked Set(...) which updates Current and raises PropertyChanged(Current)
        statusMock.Object.Set(AppStatusLevel.Warning, "Heads up", "Something happened");

        Assert.Contains(nameof(ShellViewModel.StatusLeftText), changed);
        Assert.Contains(nameof(ShellViewModel.StatusRightText), changed);
        Assert.Contains(nameof(ShellViewModel.StatusBrush), changed);

        Assert.Equal("Heads up", vm.StatusLeftText);
        Assert.Equal("Something happened", vm.StatusRightText);
    }

    // ============================================================
    // Tests: LogViewer Child VM Exposure
    // ============================================================

    /// <summary>
    /// Verifies that the shell view model properly exposes the LogViewerViewModel child instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates child view model management:
    /// <list type="bullet">
    ///   <item><description>LogViewerViewModel property returns the instance provided during construction</description></item>
    ///   <item><description>Reference equality ensures no unexpected wrapper or proxy</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The shell owns the LogViewerViewModel but allows it to operate independently with
    /// its own state management and commands, maintaining clean separation of concerns.
    /// The MainWindow XAML binds to this property for log viewer dialog functionality.
    /// </para>
    /// </remarks>
    [Fact]
    public void Exposes_LogViewerViewModel_Instance()
    {
        var vm = CreateVm(
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out var logViewerVm);

        Assert.Same(logViewerVm, vm.LogViewerViewModel);
    }

    // ============================================================
    // Helper Methods
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
    /// to each test. Properties and Scopes are set to empty arrays as they're not
    /// typically needed for these tests.
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
}