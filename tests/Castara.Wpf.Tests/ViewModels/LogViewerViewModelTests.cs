using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Contains unit tests for <see cref="LogViewerViewModel"/> to verify log viewing,
/// filtering, searching, selection management, and clipboard operations.
/// </summary>
/// <remarks>
/// <para>
/// This test suite validates the LogViewerViewModel's comprehensive log management features through behavioral testing:
/// </para>
/// <para>
/// <strong>Dialog Management Tests:</strong> Verify open/close command behavior and IsOpen property state.
/// </para>
/// <para>
/// <strong>Log Count Tracking Tests:</strong> Ensure LogCount and HasLogs properties reactively track
/// the log store's entry count for UI visibility and command enablement.
/// </para>
/// <para>
/// <strong>Sorting Tests:</strong> Validate descending timestamp sort (newest entries first) configuration.
/// </para>
/// <para>
/// <strong>Filtering Tests:</strong> Verify exact log level filtering and case-insensitive search across
/// message, category, and exception text fields.
/// </para>
/// <para>
/// <strong>Selection Tests:</strong> Validate that SelectedEntries drives CopySelected command's CanExecute
/// state, supporting both single and multi-selection scenarios.
/// </para>
/// <para>
/// <strong>Clipboard Operation Tests:</strong> Ensure CopySelected and CopyAll commands generate properly
/// formatted TSV (tab-separated values) output with header row and cleaned field values, verifying output
/// through mocked <see cref="Castara.Wpf.Services.Clipboard.IClipboardService"/>.
/// </para>
/// <para>
/// <strong>SelectAll Pattern Tests:</strong> Verify the event-based pattern for requesting DataGrid.SelectAll()
/// from the view model while maintaining MVVM separation.
/// </para>
/// <para>
/// <strong>Clear Command Tests:</strong> Validate that clearing logs updates the store, resets selection,
/// and refreshes count properties.
/// </para>
/// <para>
/// The tests use Moq for dependency isolation with strict behavior. The IClipboardService abstraction
/// enables testing clipboard operations without accessing the actual system clipboard.
/// </para>
/// </remarks>
public sealed class LogViewerViewModelTests
{
    // ============================================================
    // Factory Methods
    // ============================================================

    /// <summary>
    /// Creates a <see cref="LogViewerViewModel"/> instance with mocked dependencies for testing.
    /// </summary>
    /// <param name="backing">
    /// Outputs the backing <see cref="ObservableCollection{T}"/> that tests can manipulate directly.
    /// </param>
    /// <param name="ro">
    /// Outputs the <see cref="ReadOnlyObservableCollection{T}"/> wrapper returned by the mock store.
    /// </param>
    /// <param name="storeMock">
    /// Outputs the mocked <see cref="IObservableLogStore"/> for verification.
    /// </param>
    /// <param name="clipboardMock">
    /// Outputs the mocked <see cref="Castara.Wpf.Services.Clipboard.IClipboardService"/> for clipboard operation verification.
    /// </param>
    /// <returns>
    /// A <see cref="LogViewerViewModel"/> instance with mocked dependencies ready for testing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This factory method sets up the required mocks with strict behavior:
    /// <list type="bullet">
    ///   <item><description>Log store mock configured to return read-only wrapper and support Clear()</description></item>
    ///   <item><description>Clipboard service mock configured to capture SetText calls for verification</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The backing collection approach allows tests to add/remove entries directly while the
    /// view model sees them through the read-only wrapper, simulating real usage patterns.
    /// </para>
    /// <para>
    /// The clipboard service abstraction (<see cref="Castara.Wpf.Services.Clipboard.IClipboardService"/>)
    /// enables testing clipboard operations without accessing the actual system clipboard,
    /// which would be unreliable and could interfere with user clipboard content during test execution.
    /// </para>
    /// </remarks>
    private static LogViewerViewModel CreateVm(
        out ObservableCollection<LogEntry> backing,
        out ReadOnlyObservableCollection<LogEntry> ro,
        out Mock<IObservableLogStore> storeMock,
        out Mock<Castara.Wpf.Services.Clipboard.IClipboardService> clipboardMock)
    {
        backing = new ObservableCollection<LogEntry>();
        ro = new ReadOnlyObservableCollection<LogEntry>(backing);

        storeMock = new Mock<IObservableLogStore>(MockBehavior.Strict);
        storeMock.SetupGet(s => s.Entries).Returns(ro);
        storeMock.Setup(s => s.Clear()).Callback(backing.Clear);

        clipboardMock = new Mock<Castara.Wpf.Services.Clipboard.IClipboardService>(MockBehavior.Strict);
        clipboardMock.Setup(c => c.SetText(It.IsAny<string>()));

        return new LogViewerViewModel(storeMock.Object, clipboardMock.Object);
    }

    // ============================================================
    // Tests: Dialog Open/Close
    // ============================================================

    /// <summary>
    /// Verifies that the ShowCommand sets the IsOpen property to true, displaying the dialog.
    /// </summary>
    /// <remarks>
    /// This test validates the dialog opening mechanism. The IsOpen property is bound to
    /// MaterialDesignInXAML's DialogHost.IsOpen in the UI, controlling dialog visibility.
    /// </remarks>
    [Fact]
    public void ShowCommand_SetsIsOpen_True()
    {
        var vm = CreateVm(out _, out _, out _, out _);

        Assert.False(vm.IsOpen);
        vm.ShowCommand.Execute(null);
        Assert.True(vm.IsOpen);
    }

    /// <summary>
    /// Verifies that the CloseCommand sets the IsOpen property to false, hiding the dialog.
    /// </summary>
    /// <remarks>
    /// This test validates the dialog closing mechanism. The user can close the dialog
    /// via a close button in the dialog header that invokes this command.
    /// </remarks>
    [Fact]
    public void CloseCommand_SetsIsOpen_False()
    {
        var vm = CreateVm(out _, out _, out _, out _);

        vm.IsOpen = true;
        vm.CloseCommand.Execute(null);
        Assert.False(vm.IsOpen);
    }

    // ============================================================
    // Tests: LogCount + HasLogs Tracking
    // ============================================================

    /// <summary>
    /// Verifies that LogCount and HasLogs properties reactively track the log store's entry count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the reactive binding chain:
    /// <list type="number">
    ///   <item><description>LogCount returns the current entry count from the store</description></item>
    ///   <item><description>HasLogs returns true when entries exist, false when empty</description></item>
    ///   <item><description>Both properties update automatically as entries are added/removed</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// HasLogs is used to control the visibility of the notification badge on the log viewer button
    /// in the status bar and to enable/disable the Copy All and Clear commands.
    /// </para>
    /// </remarks>
    [Fact]
    public void LogCount_AndHasLogs_TrackStoreEntries()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        Assert.Equal(0, vm.LogCount);
        Assert.False(vm.HasLogs);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hi"));

        Assert.Equal(1, vm.LogCount);
        Assert.True(vm.HasLogs);
    }

    // ============================================================
    // Tests: Sorting (Newest First)
    // ============================================================

    /// <summary>
    /// Verifies that the constructor configures the collection view with descending timestamp sort
    /// to display newest entries first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the sort configuration by:
    /// <list type="number">
    ///   <item><description>Adding two entries with different timestamps</description></item>
    ///   <item><description>Refreshing the view to apply sorting</description></item>
    ///   <item><description>Verifying the newest entry appears first</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Newest-first ordering is important for log viewing, as users typically want to see
    /// the most recent events at the top of the list for quick access to current issues.
    /// </para>
    /// </remarks>
    [Fact]
    public void Constructor_ConfiguresView_SortedNewestFirst()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-10), LogLevel.Information, "old"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "new"));

        vm.LogEntriesView.Refresh();

        var first = vm.LogEntriesView.Cast<LogEntry>().First();
        Assert.Equal("new", first.Message);
    }

    // ============================================================
    // Tests: Filtering - Exact Level
    // ============================================================

    /// <summary>
    /// Verifies that exact log level filtering works correctly, showing only entries matching
    /// the selected level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates exact level matching (not minimum level filtering):
    /// <list type="bullet">
    ///   <item><description>Information entries are hidden when Error is selected</description></item>
    ///   <item><description>Only Error entries remain visible in the filtered view</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Exact level filtering allows users to focus on specific log levels without
    /// seeing higher or lower severity entries, useful for isolating specific diagnostic information.
    /// </para>
    /// </remarks>
    [Fact]
    public void Filter_ByExactLevel_Works()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "info"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err"));

        vm.SelectedLevelOption = vm.LevelOptions.First(o => o.Level == LogLevel.Error);
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        Assert.Single(visible);
        Assert.Equal(LogLevel.Error, visible[0].Level);
    }

    // ============================================================
    // Tests: Filtering - Search (Case-Insensitive) Across Message/Category/Exception
    // ============================================================

    /// <summary>
    /// Verifies that search text filtering matches log message content with case-insensitive comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates search behavior:
    /// <list type="bullet">
    ///   <item><description>Search term "world" matches "Hello World" (case-insensitive)</description></item>
    ///   <item><description>Only matching entries remain visible in the filtered view</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Case-insensitive search improves usability by not requiring users to match exact casing
    /// when searching for log content.
    /// </para>
    /// </remarks>
    [Fact]
    public void Filter_BySearchText_MatchesMessage_CaseInsensitive()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "Hello World", category: "A"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Information, "goodbye", category: "B"));

        vm.SearchText = "world";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        Assert.Single(visible);
        Assert.Contains("world", visible[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that search text filtering matches logger category names with case-insensitive comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates category-based search:
    /// <list type="bullet">
    ///   <item><description>Search term "wpf.app" matches category "Castara.Wpf.App" (case-insensitive)</description></item>
    ///   <item><description>Enables finding logs from specific namespaces or components</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Category search is useful for filtering logs by source component, namespace, or logger name,
    /// helping developers isolate logs from specific parts of the application.
    /// </para>
    /// </remarks>
    [Fact]
    public void Filter_BySearchText_MatchesCategory_CaseInsensitive()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "msg", category: "Castara.Wpf.App"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Information, "msg", category: "Other"));

        vm.SearchText = "wpf.app";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        Assert.Single(visible);
        Assert.Equal("Castara.Wpf.App", visible[0].Category);
    }

    /// <summary>
    /// Verifies that search text filtering matches exception text content with case-insensitive comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates exception-based search:
    /// <list type="bullet">
    ///   <item><description>Search term "thing" matches exception message "Bad THING happened" (case-insensitive)</description></item>
    ///   <item><description>Searches the entire exception ToString() output (type, message, stack trace)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Exception search is critical for finding error logs by exception type, message content,
    /// or even specific method names from stack traces, supporting effective error diagnosis.
    /// </para>
    /// </remarks>
    [Fact]
    public void Filter_BySearchText_MatchesExceptionText_CaseInsensitive()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Error, "boom", ex: new InvalidOperationException("Bad THING happened")));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "nope", ex: null));

        vm.SearchText = "thing";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        Assert.Single(visible);
        Assert.NotNull(visible[0].Exception);
        Assert.Contains("thing", visible[0].Exception!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // Tests: SelectedEntries Drives CopySelected CanExecute
    // ============================================================

    /// <summary>
    /// Verifies that CopySelected command's CanExecute returns false when no entries are selected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the command enablement logic:
    /// <list type="bullet">
    ///   <item><description>CanExecute returns false when SelectedEntries is null or empty</description></item>
    ///   <item><description>Prevents execution when there's nothing to copy</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This behavior ensures the Copy Selected button/menu item is disabled when no entries
    /// are selected, providing clear UI feedback about available actions.
    /// </para>
    /// </remarks>
    [Fact]
    public void CopySelected_CanExecute_False_WhenNoSelection()
    {
        var vm = CreateVm(out _, out _, out _, out _);

        // SelectedEntries defaults to empty
        Assert.False(vm.CopySelectedCommand.CanExecute(null));

        // Even if it's explicitly set to empty
        vm.SelectedEntries = Array.Empty<object>();
        Assert.False(vm.CopySelectedCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that CopySelected command's CanExecute returns true when entries are selected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the command enablement logic:
    /// <list type="bullet">
    ///   <item><description>CanExecute returns true when SelectedEntries contains items</description></item>
    ///   <item><description>Enables execution when there's content to copy</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This behavior ensures the Copy Selected button/menu item is enabled when one or more
    /// entries are selected, supporting both single and multi-selection copy operations.
    /// </para>
    /// </remarks>
    [Fact]
    public void CopySelected_CanExecute_True_WhenSelectionHasItems()
    {
        var vm = CreateVm(out var backing, out _, out _, out _);

        var entry = MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hi");
        backing.Add(entry);

        vm.SelectedEntries = new ArrayList { entry };

        Assert.True(vm.CopySelectedCommand.CanExecute(null));
    }

    // ============================================================
    // Tests: CopySelected / CopyAll Writes TSV to Clipboard Service
    // ============================================================

    /// <summary>
    /// Verifies that CopySelected command writes properly formatted TSV content to the clipboard
    /// service for the selected rows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the clipboard export workflow:
    /// <list type="number">
    ///   <item><description>Selected entries are extracted from SelectedEntries (supports non-contiguous selection)</description></item>
    ///   <item><description>TSV format is generated with header row (Timestamp, Level, Category, Message, Exception)</description></item>
    ///   <item><description>Field values are cleaned (tabs, newlines, carriage returns replaced with spaces)</description></item>
    ///   <item><description>Output is passed to IClipboardService.SetText for clipboard placement</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The TSV format with cleaning ensures data integrity when pasting into Excel, database tools,
    /// or text editors. Tabs and newlines within field values would corrupt the TSV structure if not replaced.
    /// </para>
    /// <para>
    /// Non-contiguous selection support allows users to select specific entries with Ctrl+Click,
    /// then copy only those entries regardless of their position in the list.
    /// </para>
    /// </remarks>
    [Fact]
    public void CopySelected_WritesTsvToClipboard_ForSelectedRows()
    {
        var vm = CreateVm(out var backing, out _, out _, out var clipboardMock);

        var a = MakeLog(DateTimeOffset.Parse("2026-03-04T12:00:00Z"), LogLevel.Information, "hello", category: "CatA");
        var b = MakeLog(DateTimeOffset.Parse("2026-03-04T12:00:01Z"), LogLevel.Error, "err\twith\ttabs", category: "CatB", ex: new Exception("boom\nline2"));

        backing.Add(a);
        backing.Add(b);

        // Non-contiguous selection supported via IList
        vm.SelectedEntries = new ArrayList { b, a };

        vm.CopySelectedCommand.Execute(null);

        clipboardMock.Verify(c => c.SetText(It.Is<string>(txt =>
            txt.StartsWith("Timestamp\tLevel\tCategory\tMessage\tException" + Environment.NewLine, StringComparison.Ordinal)
            && txt.Contains("CatA", StringComparison.Ordinal)
            && txt.Contains("CatB", StringComparison.Ordinal)
            // Ensure cleaning happened (tabs/newlines replaced)
            && !txt.Contains("\twith\ttabs", StringComparison.Ordinal)
            && !txt.Contains("boom\nline2", StringComparison.Ordinal)
        )), Times.Once);
    }

    /// <summary>
    /// Verifies that CopyAll command writes properly formatted TSV content to the clipboard service
    /// for all filtered rows (respecting current level and search filters).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the "copy all visible" workflow:
    /// <list type="number">
    ///   <item><description>Applies a filter (Error level only in this test)</description></item>
    ///   <item><description>CopyAll extracts all entries from LogEntriesView (filtered collection)</description></item>
    ///   <item><description>Only filtered entries are included in the TSV output</description></item>
    ///   <item><description>Filtered-out entries (Information in this test) are excluded</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This behavior allows users to filter logs to a specific subset (e.g., only errors from a
    /// specific component), then copy all matching entries for analysis in external tools,
    /// without manually selecting each entry.
    /// </para>
    /// <para>
    /// The test verifies both positive (Error present) and negative (Information absent) conditions
    /// to ensure filtering is correctly applied before copying.
    /// </para>
    /// </remarks>
    [Fact]
    public void CopyAll_WritesTsvToClipboard_ForAllFilteredRows()
    {
        var vm = CreateVm(out var backing, out _, out _, out var clipboardMock);

        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-2), LogLevel.Information, "info", category: "A"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err", category: "B"));

        // Filter to Error only; CopyAll should copy only visible rows
        vm.SelectedLevelOption = vm.LevelOptions.First(o => o.Level == LogLevel.Error);
        vm.LogEntriesView.Refresh();

        Assert.Single(vm.LogEntriesView.Cast<LogEntry>());

        vm.CopyAllCommand.Execute(null);

        clipboardMock.Verify(c => c.SetText(It.Is<string>(txt =>
            txt.Contains("\tError\t", StringComparison.Ordinal)
            && !txt.Contains("\tInformation\t", StringComparison.Ordinal)
        )), Times.Once);
    }

    // ============================================================
    // Tests: SelectAll Event Pattern
    // ============================================================

    /// <summary>
    /// Verifies that the SelectAllCommand raises the RequestSelectAll event for the view to handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the event-based pattern for SelectAll:
    /// <list type="number">
    ///   <item><description>View subscribes to RequestSelectAll event</description></item>
    ///   <item><description>View model executes SelectAllCommand</description></item>
    ///   <item><description>RequestSelectAll event is raised</description></item>
    ///   <item><description>View handles event by calling DataGrid.SelectAll()</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This event-based pattern maintains MVVM separation while allowing the view model to
    /// trigger a UI operation (SelectAll) that requires direct DataGrid manipulation.
    /// The view owns the DataGrid and responds to the event, avoiding code-behind coupling
    /// while still supporting the SelectAll feature.
    /// </para>
    /// </remarks>
    [Fact]
    public void SelectAllCommand_RaisesRequestSelectAllEvent()
    {
        var vm = CreateVm(out _, out _, out _, out _);

        var raised = false;
        vm.RequestSelectAll += (_, __) => raised = true;

        vm.SelectAllCommand.Execute(null);

        Assert.True(raised);
    }

    // ============================================================
    // Tests: ClearCommand
    // ============================================================

    /// <summary>
    /// Verifies that the ClearCommand clears the log store, resets selection properties,
    /// and refreshes count properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test validates the complete clear workflow:
    /// <list type="number">
    ///   <item><description>Calls Clear() on the log store (verified via mock)</description></item>
    ///   <item><description>Resets SelectedLogEntry to null</description></item>
    ///   <item><description>Updates LogCount to 0 (count property automatically reflects store state)</description></item>
    ///   <item><description>Updates HasLogs to false</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Clearing logs is important for managing memory usage during long-running sessions
    /// and resetting the diagnostic state. The command should show a confirmation dialog
    /// in the view before execution to prevent accidental data loss.
    /// </para>
    /// <para>
    /// Resetting the selection prevents stale references to cleared entries that would
    /// cause UI errors or null reference exceptions.
    /// </para>
    /// </remarks>
    [Fact]
    public void ClearCommand_ClearsStore_ResetsSelection_AndRefreshesCounts()
    {
        var vm = CreateVm(out var backing, out _, out var storeMock, out _);

        backing.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "a"));
        backing.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "b"));

        vm.SelectedLogEntry = backing[0];
        vm.SelectedEntries = new ArrayList { backing[0] };

        Assert.True(vm.HasLogs);
        Assert.Equal(2, vm.LogCount);
        Assert.NotNull(vm.SelectedLogEntry);

        vm.ClearCommand.Execute(null);

        storeMock.Verify(s => s.Clear(), Times.Once);

        Assert.Equal(0, vm.LogCount);
        Assert.False(vm.HasLogs);
        Assert.Null(vm.SelectedLogEntry);
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
