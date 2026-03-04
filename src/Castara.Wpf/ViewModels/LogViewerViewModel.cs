using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Castara.Wpf.Infrastructure.Commands;
using Castara.Wpf.Infrastructure.Telemetry.Logging;
using Castara.Wpf.Services.Clipboard;
using Microsoft.Extensions.Logging;

namespace Castara.Wpf.ViewModels;

/// <summary>
/// The view model for the log viewer dialog, managing log display, filtering, searching,
/// selection, and clipboard operations for application diagnostic logs.
/// </summary>
public sealed class LogViewerViewModel : INotifyPropertyChanged
{
    // ============================================================
    // Fields
    // ============================================================

    private readonly IObservableLogStore _logStore;
    private readonly IClipboardService _clipboard;

    private bool _isOpen;
    private string _searchText = string.Empty;
    private LogLevelOption _selectedLevelOption = LogLevelOption.All;
    private LogEntry? _selectedLogEntry;
    private IList _selectedEntries = Array.Empty<object>();

    // ============================================================
    // Events
    // ============================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when the view model requests the view to select all items in the DataGrid.
    /// The view subscribes and calls DataGrid.SelectAll().
    /// </summary>
    public event EventHandler? RequestSelectAll;

    // ============================================================
    // Constructor
    // ============================================================

    public LogViewerViewModel(IObservableLogStore logStore, IClipboardService clipboard)
    {
        _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));

        // Create filtered/sorted view over log store
        LogEntriesView = CollectionViewSource.GetDefaultView(_logStore.Entries);
        LogEntriesView.Filter = LogFilter;

        // Sort newest first
        LogEntriesView.SortDescriptions.Clear();
        LogEntriesView.SortDescriptions.Add(
            new SortDescription(nameof(LogEntry.Timestamp), ListSortDirection.Descending));

        // React to collection changes for counts + refresh
        if (_logStore.Entries is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += (_, __) =>
            {
                Notify(nameof(LogCount));
                Notify(nameof(HasLogs));
                LogEntriesView.Refresh();
                RaiseCanExecutes();
            };
        }

        // Commands
        ShowCommand = new RelayCommand(() => IsOpen = true);
        CloseCommand = new RelayCommand(() => IsOpen = false);

        ClearCommand = new RelayCommand(() =>
        {
            _logStore.Clear();
            SelectedLogEntry = null;
            SelectedEntries = Array.Empty<object>();

            Notify(nameof(LogCount));
            Notify(nameof(HasLogs));

            LogEntriesView.Refresh();
            RaiseCanExecutes();
        });

        CopySelectedCommand = new RelayCommand(CopySelected, CanCopySelected);
        CopyAllCommand = new RelayCommand(CopyAll, () => HasLogs);

        SelectAllCommand = new RelayCommand(() =>
        {
            RequestSelectAll?.Invoke(this, EventArgs.Empty);
        });
    }

    // ============================================================
    // Properties - Dialog State
    // ============================================================

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value) return;
            _isOpen = value;
            Notify(nameof(IsOpen));
        }
    }

    // ============================================================
    // Properties - Log Store and View
    // ============================================================

    public IObservableLogStore LogStore => _logStore;

    public ICollectionView LogEntriesView { get; }

    public int LogCount => _logStore.Entries.Count;

    public bool HasLogs => LogCount > 0;

    // ============================================================
    // Properties - Filtering
    // ============================================================

    public string SearchText
    {
        get => _searchText;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_searchText, next, StringComparison.Ordinal)) return;

            _searchText = next;
            Notify(nameof(SearchText));
            LogEntriesView.Refresh();
            RaiseCanExecutes();
        }
    }

    public IReadOnlyList<LogLevelOption> LevelOptions { get; } = new[]
    {
        LogLevelOption.All,
        new LogLevelOption("Trace", LogLevel.Trace),
        new LogLevelOption("Debug", LogLevel.Debug),
        new LogLevelOption("Information", LogLevel.Information),
        new LogLevelOption("Warning", LogLevel.Warning),
        new LogLevelOption("Error", LogLevel.Error),
        new LogLevelOption("Critical", LogLevel.Critical),
    };

    public LogLevelOption SelectedLevelOption
    {
        get => _selectedLevelOption;
        set
        {
            if (Equals(_selectedLevelOption, value)) return;

            _selectedLevelOption = value;
            Notify(nameof(SelectedLevelOption));
            LogEntriesView.Refresh();
            RaiseCanExecutes();
        }
    }

    // ============================================================
    // Properties - Selection
    // ============================================================

    public LogEntry? SelectedLogEntry
    {
        get => _selectedLogEntry;
        set
        {
            if (Equals(_selectedLogEntry, value)) return;
            _selectedLogEntry = value;
            Notify(nameof(SelectedLogEntry));
        }
    }

    /// <summary>
    /// Bound from DataGrid.SelectedItems via attached behavior (supports multi-select).
    /// </summary>
    public IList SelectedEntries
    {
        get => _selectedEntries;
        set
        {
            _selectedEntries = value ?? Array.Empty<object>();
            Notify(nameof(SelectedEntries));
            RaiseCanExecutes();
        }
    }

    // ============================================================
    // Commands
    // ============================================================

    public ICommand ShowCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ClearCommand { get; }

    public ICommand CopySelectedCommand { get; }
    public ICommand CopyAllCommand { get; }

    public ICommand SelectAllCommand { get; }

    // ============================================================
    // Private Methods - Filtering
    // ============================================================

    private bool LogFilter(object obj)
    {
        if (obj is not LogEntry entry)
            return false;

        // Exact level filter (not minimum)
        var level = SelectedLevelOption.Level;
        if (level is { } l && entry.Level != l)
            return false;

        // Search
        var q = (SearchText ?? string.Empty).Trim();
        if (q.Length == 0)
            return true;

        return Contains(entry.Message, q)
               || Contains(entry.Category, q)
               || (entry.Exception != null && Contains(entry.Exception.ToString(), q));
    }

    private static bool Contains(string? haystack, string needle)
        => !string.IsNullOrEmpty(haystack)
           && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    // ============================================================
    // Private Methods - Copy
    // ============================================================

    private bool CanCopySelected()
        => SelectedEntries is { Count: > 0 };

    private void CopySelected()
    {
        var rows = SelectedEntries
            .Cast<object>()
            .OfType<LogEntry>()
            .ToList();

        CopyRowsToClipboard(rows);
    }

    private void CopyAll()
    {
        var rows = LogEntriesView.Cast<LogEntry>().ToList();
        CopyRowsToClipboard(rows);
    }

    private void CopyRowsToClipboard(IReadOnlyList<LogEntry> rows)
    {
        if (rows.Count == 0)
            return;

        static string Clean(string? s)
        {
            var x = s ?? string.Empty;
            return x.Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " ");
        }

        const string header = "Timestamp\tLevel\tCategory\tMessage\tException";

        var lines = rows.Select(r =>
            $"{r.Timestamp:O}\t{r.Level}\t{Clean(r.Category)}\t{Clean(r.Message)}\t{Clean(r.Exception?.ToString())}");

        var text = header + Environment.NewLine + string.Join(Environment.NewLine, lines);

        _clipboard.SetText(text);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private void RaiseCanExecutes()
    {
        if (CopySelectedCommand is RelayCommand r1)
            r1.RaiseCanExecuteChanged();

        if (CopyAllCommand is RelayCommand r2)
            r2.RaiseCanExecuteChanged();
    }

    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ============================================================
    // Nested Types
    // ============================================================

    public sealed record LogLevelOption(string Display, LogLevel? Level)
    {
        public static LogLevelOption All { get; } = new("All", null);
        public override string ToString() => Display;
    }
}