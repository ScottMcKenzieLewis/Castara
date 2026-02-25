using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Wpf.Infrastructure.Converters;

/// <summary>
/// Converts <see cref="RiskSeverity"/> enum values to WPF <see cref="Brush"/> objects
/// for visual representation in the user interface.
/// </summary>
/// <remarks>
/// <para>
/// This converter maps risk severity levels to color-coded brushes that provide
/// intuitive visual feedback about the importance and urgency of risk flags.
/// All brushes are frozen for optimal performance in data binding scenarios.
/// </para>
/// <para>
/// Color mappings follow iOS/macOS system color conventions:
/// <list type="table">
///   <listheader>
///     <term>Severity</term>
///     <description>Color (Hex)</description>
///   </listheader>
///   <item>
///     <term><see cref="RiskSeverity.Low"/></term>
///     <description>Green (#35C759) - Acceptable conditions</description>
///   </item>
///   <item>
///     <term><see cref="RiskSeverity.Medium"/></term>
///     <description>Yellow (#FFCC00) - Requires monitoring</description>
///   </item>
///   <item>
///     <term><see cref="RiskSeverity.High"/></term>
///     <description>Red (#FF3B30) - Immediate attention required</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// This converter is registered in XAML resources and used for binding risk flag
/// severity indicators, borders, and text colors throughout the application.
/// </para>
/// </remarks>
/// <example>
/// Example usage in XAML:
/// <code>
/// &lt;UserControl.Resources&gt;
///     &lt;conv:RiskSeverityToBrushConverter x:Key="SeverityBrushConverter"/&gt;
/// &lt;/UserControl.Resources&gt;
/// 
/// &lt;!-- Bind border color to severity --&gt;
/// &lt;Border BorderBrush="{Binding Severity, Converter={StaticResource SeverityBrushConverter}}"
///         BorderThickness="1"&gt;
///     &lt;TextBlock Text="{Binding Message}"/&gt;
/// &lt;/Border&gt;
/// </code>
/// </example>
public sealed class RiskSeverityToBrushConverter : IValueConverter
{
    /// <summary>
    /// Brush for low severity risk flags (green).
    /// </summary>
    private static readonly Brush Low = Make("#35C759");

    /// <summary>
    /// Brush for medium severity risk flags (yellow).
    /// </summary>
    private static readonly Brush Medium = Make("#FFCC00");

    /// <summary>
    /// Brush for high severity risk flags (red).
    /// </summary>
    private static readonly Brush High = Make("#FF3B30");

    /// <summary>
    /// Default brush for unknown or invalid severity values (gray).
    /// </summary>
    private static readonly Brush Default = Brushes.Gray;

    /// <summary>
    /// Creates a frozen <see cref="SolidColorBrush"/> from a hexadecimal color string.
    /// </summary>
    /// <param name="hex">The hexadecimal color string (e.g., "#35C759").</param>
    /// <returns>A frozen <see cref="Brush"/> instance for optimal performance.</returns>
    /// <remarks>
    /// Freezing brushes prevents modifications and improves performance by allowing
    /// the brush to be shared across threads and eliminating dependency property overhead.
    /// </remarks>
    private static Brush Make(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        if (brush.CanFreeze)
            brush.Freeze();
        return brush;
    }

    /// <summary>
    /// Converts a <see cref="RiskSeverity"/> value to a corresponding <see cref="Brush"/>.
    /// </summary>
    /// <param name="value">
    /// The value to convert, expected to be a <see cref="RiskSeverity"/> enum value.
    /// </param>
    /// <param name="targetType">
    /// The type of the binding target property (not used in this converter).
    /// </param>
    /// <param name="parameter">
    /// Optional converter parameter (not used in this converter).
    /// </param>
    /// <param name="culture">
    /// The culture to use in the converter (not used in this converter).
    /// </param>
    /// <returns>
    /// A <see cref="Brush"/> corresponding to the severity level:
    /// <list type="bullet">
    ///   <item><description>Green brush for <see cref="RiskSeverity.Low"/></description></item>
    ///   <item><description>Yellow brush for <see cref="RiskSeverity.Medium"/></description></item>
    ///   <item><description>Red brush for <see cref="RiskSeverity.High"/></description></item>
    ///   <item><description>Gray brush for invalid or unknown values</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// If the input value is not a valid <see cref="RiskSeverity"/> enum value,
    /// the converter returns a gray brush as a fallback.
    /// </remarks>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RiskSeverity severity)
            return Default;

        return severity switch
        {
            RiskSeverity.Low => Low,
            RiskSeverity.Medium => Medium,
            RiskSeverity.High => High,
            _ => Default
        };
    }

    /// <summary>
    /// Converts a brush back to a <see cref="RiskSeverity"/> value.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Not supported - always throws.</returns>
    /// <exception cref="NotSupportedException">
    /// This converter only supports one-way binding from severity to brush.
    /// Two-way binding is not supported.
    /// </exception>
    /// <remarks>
    /// Converting from a brush back to a severity level is not meaningful in this
    /// context, as the converter is designed for one-way display purposes only.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("RiskSeverityToBrushConverter only supports one-way conversion.");
}