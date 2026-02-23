using Castara.Wpf.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Input;

namespace Castara.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // Ensure icon matches initial state
        UpdateMaxRestoreIcon();
    }

    // Drag window; double-click toggles maximize/restore
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaxRestore();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    // Prevent titlebar drag handler from swallowing caption button clicks
    private void CaptionButtons_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => SystemCommands.MinimizeWindow(this);

    private void Close_Click(object sender, RoutedEventArgs e)
        => SystemCommands.CloseWindow(this);

    private void MaxRestore_Click(object sender, RoutedEventArgs e)
        => ToggleMaxRestore();

    private void ToggleMaxRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateMaxRestoreIcon();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateMaxRestoreIcon();
    }

    private void UpdateMaxRestoreIcon()
    {
        if (MaxRestoreIcon is null) return;

        MaxRestoreIcon.Kind = WindowState == WindowState.Maximized
            ? PackIconKind.WindowRestore
            : PackIconKind.WindowMaximize;
    }
}