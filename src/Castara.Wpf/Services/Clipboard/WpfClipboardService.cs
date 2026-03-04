using System.Windows;

namespace Castara.Wpf.Services.Clipboard;

/// <summary>
/// Production implementation of <see cref="IClipboardService"/> that delegates to the
/// WPF <see cref="System.Windows.Clipboard"/> API for actual system clipboard access.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a thin wrapper around <see cref="System.Windows.Clipboard"/>
/// to enable dependency injection and testability in the Castara application. The
/// abstraction allows:
/// <list type="bullet">
///   <item><description>View models to depend on <see cref="IClipboardService"/> instead of static APIs</description></item>
///   <item><description>Unit tests to mock clipboard operations without accessing the system clipboard</description></item>
///   <item><description>Consistent service patterns throughout the application</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Registration:</strong> This service should be registered in the dependency injection
/// container as a singleton, typically in <c>App.xaml.cs</c> during application startup:
/// <code>
/// services.AddSingleton&lt;IClipboardService, WpfClipboardService&gt;();
/// </code>
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> The WPF Clipboard API requires STA (Single-Threaded Apartment)
/// thread access. This implementation must be called from the UI thread or a thread with STA
/// apartment state. Background thread calls will throw <see cref="System.Threading.ThreadStateException"/>.
/// </para>
/// <para>
/// <strong>Usage Example:</strong> In <see cref="ViewModels.LogViewerViewModel"/>, this service
/// is injected and used to copy log entries in TSV format:
/// <code>
/// public LogViewerViewModel(IObservableLogStore logStore, IClipboardService clipboard)
/// {
///     _clipboard = clipboard;
///     // ... later in CopySelected command:
///     _clipboard.SetText(tsvFormattedLogs);
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class WpfClipboardService : IClipboardService
{
    /// <summary>
    /// Sets the clipboard to contain the specified text data by delegating to
    /// <see cref="System.Windows.Clipboard.SetText(string)"/>.
    /// </summary>
    /// <param name="text">The text data to place on the clipboard.</param>
    /// <exception cref="System.Threading.ThreadStateException">
    /// Thrown if called from a non-STA thread (must be called from UI thread).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method places the specified text on the Windows system clipboard, replacing
    /// any existing content. The text will be available to other applications through
    /// standard clipboard operations (Ctrl+V, paste context menu, etc.).
    /// </para>
    /// <para>
    /// <strong>Thread Requirements:</strong> Must be called from the UI thread or a thread
    /// with STA apartment state. The WPF Dispatcher can be used to marshal calls if needed:
    /// <code>
    /// Application.Current.Dispatcher.Invoke(() => clipboard.SetText(text));
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Security:</strong> Clipboard operations may be restricted in partial trust
    /// environments or by security policies. The underlying WPF API will throw appropriate
    /// exceptions if clipboard access is denied.
    /// </para>
    /// </remarks>
    public void SetText(string text) => System.Windows.Clipboard.SetText(text);
}