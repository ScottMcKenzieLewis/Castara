namespace Castara.Wpf.Services.Clipboard;

/// <summary>
/// Provides an abstraction over clipboard operations for testability and dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// This interface wraps the static <see cref="System.Windows.Clipboard"/> API to enable:
/// <list type="bullet">
///   <item><description>Unit testing of view models without accessing the actual system clipboard</description></item>
///   <item><description>Dependency injection of clipboard functionality</description></item>
///   <item><description>Mock implementations for automated testing scenarios</description></item>
///   <item><description>Alternative clipboard implementations if needed (e.g., in-memory for testing)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong> Inject this interface into view models or services that need
/// clipboard access. The production implementation delegates to <see cref="System.Windows.Clipboard"/>,
/// while test implementations can use mocks to verify clipboard operations without system interaction.
/// </para>
/// <para>
/// <strong>Example:</strong> The LogViewerViewModel uses this interface to copy log entries to the
/// clipboard in TSV format, allowing tests to verify the copy operation without actually accessing
/// the system clipboard.
/// </para>
/// </remarks>
public interface IClipboardService
{
    /// <summary>
    /// Sets the clipboard to contain the specified text data.
    /// </summary>
    /// <param name="text">The text data to place on the clipboard.</param>
    /// <remarks>
    /// <para>
    /// This method places the specified text on the clipboard, replacing any existing content.
    /// The text will be available to other applications through standard clipboard operations
    /// (Ctrl+V, paste, etc.).
    /// </para>
    /// <para>
    /// <strong>Implementation Notes:</strong>
    /// <list type="bullet">
    ///   <item><description>Production implementation should delegate to <see cref="System.Windows.Clipboard.SetText(string)"/></description></item>
    ///   <item><description>Test implementations can store the text for verification or ignore it</description></item>
    ///   <item><description>Implementations should handle null or empty strings appropriately</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> The WPF Clipboard API requires STA thread access. Implementations
    /// should ensure proper thread marshalling if called from background threads.
    /// </para>
    /// </remarks>
    void SetText(string text);
}