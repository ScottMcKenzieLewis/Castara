using System;
using System.Collections;
using System.Linq;
using System.Windows.Data;
using Castara.Wpf.Diagnostics.Telemetry.Logging;
using Castara.Wpf.Tests.Common;
using Castara.Wpf.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Castara.Wpf.Tests.ViewModels;

/// <summary>
/// Contains unit tests for <see cref="LogViewerViewModel"/> to verify log viewing,
/// filtering, searching, selection management, and clipboard operations.
/// </summary>
public sealed class LogViewerViewModelTests
{
    [Fact]
    public void ShowCommand_SetsIsOpen_True()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        vm.IsOpen.Should().BeFalse();

        vm.ShowCommand.Execute(null);

        vm.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void CloseCommand_SetsIsOpen_False()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        vm.IsOpen = true;

        vm.CloseCommand.Execute(null);

        vm.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void LogCount_AndHasLogs_TrackStoreEntries()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        vm.LogCount.Should().Be(0);
        vm.HasLogs.Should().BeFalse();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hi"));

        vm.LogCount.Should().Be(1);
        vm.HasLogs.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ConfiguresView_SortedNewestFirst()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-10), LogLevel.Information, "old"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "new"));

        vm.LogEntriesView.Refresh();

        var first = vm.LogEntriesView.Cast<LogEntry>().First();
        first.Message.Should().Be("new");
    }

    [Fact]
    public void Filter_ByExactLevel_Works()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "info"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err"));

        vm.SelectedLevelOption = vm.LevelOptions.First(o => o.Level == LogLevel.Error);
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        visible.Should().HaveCount(1);
        visible[0].Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void Filter_BySearchText_MatchesMessage_CaseInsensitive()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "Hello World", category: "A"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Information, "goodbye", category: "B"));

        vm.SearchText = "world";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        visible.Should().HaveCount(1);
        visible[0].Message.Should().ContainEquivalentOf("world");
    }

    [Fact]
    public void Filter_BySearchText_MatchesCategory_CaseInsensitive()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "msg", category: "Castara.Wpf.App"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Information, "msg", category: "Other"));

        vm.SearchText = "wpf.app";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        visible.Should().HaveCount(1);
        visible[0].Category.Should().Be("Castara.Wpf.App");
    }

    [Fact]
    public void Filter_BySearchText_MatchesExceptionText_CaseInsensitive()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(
            DateTimeOffset.UtcNow,
            LogLevel.Error,
            "boom",
            ex: new InvalidOperationException("Bad THING happened")));

        ctx.LogEntriesBacking.Add(MakeLog(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            LogLevel.Error,
            "nope",
            ex: null));

        vm.SearchText = "thing";
        vm.LogEntriesView.Refresh();

        var visible = vm.LogEntriesView.Cast<LogEntry>().ToList();
        visible.Should().HaveCount(1);
        visible[0].Exception.Should().NotBeNull();
        visible[0].Exception!.ToString().Should().ContainEquivalentOf("thing");
    }

    [Fact]
    public void CopySelected_CanExecute_False_WhenNoSelection()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        vm.CopySelectedCommand.CanExecute(null).Should().BeFalse();

        vm.SelectedEntries = Array.Empty<object>();

        vm.CopySelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CopySelected_CanExecute_True_WhenSelectionHasItems()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        var entry = MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "hi");
        ctx.LogEntriesBacking.Add(entry);

        vm.SelectedEntries = new ArrayList { entry };

        vm.CopySelectedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CopySelected_WritesTsvToClipboard_ForSelectedRows()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        var a = MakeLog(
            DateTimeOffset.Parse("2026-03-04T12:00:00Z"),
            LogLevel.Information,
            "hello",
            category: "CatA");

        var b = MakeLog(
            DateTimeOffset.Parse("2026-03-04T12:00:01Z"),
            LogLevel.Error,
            "err\twith\ttabs",
            category: "CatB",
            ex: new Exception("boom\nline2"));

        ctx.LogEntriesBacking.Add(a);
        ctx.LogEntriesBacking.Add(b);

        vm.SelectedEntries = new ArrayList { b, a };

        vm.CopySelectedCommand.Execute(null);

        ctx.Clipboard.Verify(c => c.SetText(It.Is<string>(txt =>
            txt.StartsWith("Timestamp\tLevel\tCategory\tMessage\tException" + Environment.NewLine, StringComparison.Ordinal)
            && txt.Contains("CatA", StringComparison.Ordinal)
            && txt.Contains("CatB", StringComparison.Ordinal)
            && !txt.Contains("\twith\ttabs", StringComparison.Ordinal)
            && !txt.Contains("boom\nline2", StringComparison.Ordinal)
        )), Times.Once);
    }

    [Fact]
    public void CopyAll_WritesTsvToClipboard_ForAllFilteredRows()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-2), LogLevel.Information, "info", category: "A"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "err", category: "B"));

        vm.SelectedLevelOption = vm.LevelOptions.First(o => o.Level == LogLevel.Error);
        vm.LogEntriesView.Refresh();

        vm.LogEntriesView.Cast<LogEntry>().Should().HaveCount(1);

        vm.CopyAllCommand.Execute(null);

        ctx.Clipboard.Verify(c => c.SetText(It.Is<string>(txt =>
            txt.Contains("\tError\t", StringComparison.Ordinal)
            && !txt.Contains("\tInformation\t", StringComparison.Ordinal)
        )), Times.Once);
    }

    [Fact]
    public void SelectAllCommand_RaisesRequestSelectAllEvent()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        var raised = false;
        vm.RequestSelectAll += (_, __) => raised = true;

        vm.SelectAllCommand.Execute(null);

        raised.Should().BeTrue();
    }

    [Fact]
    public void ClearCommand_ClearsStore_ResetsSelection_AndRefreshesCounts()
    {
        var ctx = new TestContext();
        var vm = ctx.CreateLogViewerViewModel();

        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow, LogLevel.Information, "a"));
        ctx.LogEntriesBacking.Add(MakeLog(DateTimeOffset.UtcNow.AddSeconds(-1), LogLevel.Error, "b"));

        vm.SelectedLogEntry = ctx.LogEntriesBacking[0];
        vm.SelectedEntries = new ArrayList { ctx.LogEntriesBacking[0] };

        vm.HasLogs.Should().BeTrue();
        vm.LogCount.Should().Be(2);
        vm.SelectedLogEntry.Should().NotBeNull();

        vm.ClearCommand.Execute(null);

        ctx.LogStore.Verify(s => s.Clear(), Times.Once);

        vm.LogCount.Should().Be(0);
        vm.HasLogs.Should().BeFalse();
        vm.SelectedLogEntry.Should().BeNull();
    }

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