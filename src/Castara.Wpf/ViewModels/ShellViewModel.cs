using System.ComponentModel;
using System.Windows.Media;
using Castara.Wpf.Models;
using Castara.Wpf.Services.Status;
using Castara.Wpf.Services.Theme;
using Castara.Wpf.ViewModels;
using Castara.Wpf.Views;

namespace Castara.Wpf.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ThemeService _themeService;
    private readonly IStatusService _statusService;

    public ShellViewModel(
        ThemeService themeService,
        IStatusService statusService,
        Views.CalculationsView calculationsView,
        CalculationsViewModel calculationsViewModel)
    {
        _themeService = themeService;
        _statusService = statusService;

        // Ensure the view actually has something to bind to at runtime
        calculationsView.DataContext = calculationsViewModel;

        CurrentView = calculationsView;

        _statusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IStatusService.Current))
            {
                Notify(nameof(StatusLeftText));
                Notify(nameof(StatusRightText));
                Notify(nameof(StatusBrush));
            }
        };

        IsDarkMode = true;
        _themeService.SetDark(true);

        _statusService.Set(AppStatusLevel.Ok, "Ready", "SQLite • Local");
    }

    // -----------------------------
    // Hosted view
    // -----------------------------

    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            if (Equals(_currentView, value)) return;
            _currentView = value;
            Notify(nameof(CurrentView));
        }
    }

    // -----------------------------
    // Theme toggle
    // -----------------------------

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;

            _isDarkMode = value;
            _themeService.SetDark(value);
            Notify(nameof(IsDarkMode));
        }
    }

    // -----------------------------
    // Status bindings
    // -----------------------------

    public string StatusLeftText => _statusService.Current.LeftText;
    public string StatusRightText => _statusService.Current.RightText;

    public Brush StatusBrush =>
        _statusService.Current.Level switch
        {
            AppStatusLevel.Ok => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35C759")),
            AppStatusLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
            AppStatusLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3B30")),
            _ => Brushes.Gray
        };

    private void Notify(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}