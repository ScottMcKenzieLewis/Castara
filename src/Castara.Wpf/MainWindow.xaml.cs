using Castara.Wpf.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Input;

namespace Castara.Wpf;

/// <summary>
/// The main application window providing custom window chrome with Material Design styling
/// and integrated navigation for cast iron analysis features.
/// </summary>
/// <remarks>
/// This window implements custom title bar behavior including:
/// <list type="bullet">
///   <item><description>Window dragging via title bar click-and-drag</description></item>
///   <item><description>Double-click to maximize/restore</description></item>
///   <item><description>Custom minimize, maximize/restore, and close buttons</description></item>
///   <item><description>Dynamic icon updates based on window state</description></item>
/// </list>
/// The custom chrome allows for Material Design theming while maintaining standard
/// Windows window management behavior.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="vm">
    /// The shell view model that manages navigation and application-level state.
    /// </param>
    /// <remarks>
    /// The view model is injected via dependency injection and set as the window's DataContext
    /// to enable data binding. The maximize/restore icon is initialized to match the current
    /// window state.
    /// </remarks>
    public MainWindow(ShellViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // Ensure icon matches initial state
        UpdateMaxRestoreIcon();
    }

    /// <summary>
    /// Handles mouse button down events on the title bar to support window dragging
    /// and double-click maximize/restore behavior.
    /// </summary>
    /// <param name="sender">The title bar element that raised the event.</param>
    /// <param name="e">
    /// The mouse button event arguments containing click count and button state.
    /// </param>
    /// <remarks>
    /// <para>
    /// This handler provides two behaviors:
    /// <list type="bullet">
    ///   <item><description>Double-click toggles between maximized and normal window states</description></item>
    ///   <item><description>Single click-and-drag moves the window</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The drag functionality is disabled when the window is maximized to match
    /// standard Windows behavior.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Prevents the title bar mouse handler from intercepting clicks on caption buttons
    /// (minimize, maximize/restore, close).
    /// </summary>
    /// <param name="sender">The caption button container that raised the event.</param>
    /// <param name="e">The mouse button event arguments.</param>
    /// <remarks>
    /// By marking the event as handled, this prevents the title bar drag handler from
    /// interfering with button clicks, ensuring the minimize, maximize, and close
    /// buttons respond correctly.
    /// </remarks>
    private void CaptionButtons_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    /// <summary>
    /// Handles the minimize button click event, minimizing the window to the taskbar.
    /// </summary>
    /// <param name="sender">The minimize button that raised the event.</param>
    /// <param name="e">The routed event arguments.</param>
    private void Minimize_Click(object sender, RoutedEventArgs e)
        => SystemCommands.MinimizeWindow(this);

    /// <summary>
    /// Handles the close button click event, closing the application window.
    /// </summary>
    /// <param name="sender">The close button that raised the event.</param>
    /// <param name="e">The routed event arguments.</param>
    /// <remarks>
    /// This triggers normal window closing behavior, including any close event handlers
    /// and application shutdown logic if this is the main window.
    /// </remarks>
    private void Close_Click(object sender, RoutedEventArgs e)
        => SystemCommands.CloseWindow(this);

    /// <summary>
    /// Handles the maximize/restore button click event, toggling between maximized
    /// and normal window states.
    /// </summary>
    /// <param name="sender">The maximize/restore button that raised the event.</param>
    /// <param name="e">The routed event arguments.</param>
    private void MaxRestore_Click(object sender, RoutedEventArgs e)
        => ToggleMaxRestore();

    /// <summary>
    /// Toggles the window between maximized and normal states, updating the
    /// maximize/restore button icon accordingly.
    /// </summary>
    /// <remarks>
    /// This method is called by both the maximize/restore button click and
    /// double-click on the title bar, providing consistent behavior across
    /// different interaction methods.
    /// </remarks>
    private void ToggleMaxRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateMaxRestoreIcon();
    }

    /// <summary>
    /// Responds to window state changes (Normal, Minimized, Maximized) by updating
    /// the maximize/restore button icon.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// This override ensures the button icon is updated when the window state changes
    /// through any mechanism (keyboard shortcuts, taskbar, etc.), not just button clicks.
    /// </remarks>
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateMaxRestoreIcon();
    }

    /// <summary>
    /// Updates the maximize/restore button icon to reflect the current window state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The icon displays:
    /// <list type="bullet">
    ///   <item><description><see cref="PackIconKind.WindowRestore"/> when maximized (clicking will restore)</description></item>
    ///   <item><description><see cref="PackIconKind.WindowMaximize"/> when normal (clicking will maximize)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The null check prevents errors during initialization before the XAML elements are loaded.
    /// </para>
    /// </remarks>
    private void UpdateMaxRestoreIcon()
    {
        if (MaxRestoreIcon is null) return;

        MaxRestoreIcon.Kind = WindowState == WindowState.Maximized
            ? PackIconKind.WindowRestore
            : PackIconKind.WindowMaximize;
    }
}