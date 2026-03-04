namespace Castara.Wpf.Models;

/// <summary>
/// Defines the available unit systems for displaying and validating section parameters
/// in the Castara application.
/// </summary>
/// <remarks>
/// <para>
/// This enum controls the presentation and validation of thickness and cooling rate parameters
/// in the calculations view. The unit system selection affects:
/// <list type="bullet">
///   <item><description>Input validation ranges (displayed in appropriate units)</description></item>
///   <item><description>Text field formatting and parsing</description></item>
///   <item><description>Tooltip and label text</description></item>
///   <item><description>User interface display values</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> Regardless of the selected unit system, all internal calculations
/// and storage use canonical SI units (mm, °C/s). The unit system only affects the presentation
/// layer. Values are converted bidirectionally between display units and canonical SI units.
/// </para>
/// <para>
/// Composition parameters (wt%) are always displayed in the same units regardless of this setting,
/// as weight percentages are universal.
/// </para>
/// </remarks>
public enum UnitSystem
{
    /// <summary>
    /// Standard (SI/Metric) unit system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system uses international standard (SI) units:
    /// <list type="bullet">
    ///   <item><description><strong>Thickness:</strong> millimeters (mm)</description></item>
    ///   <item><description><strong>Cooling Rate:</strong> degrees Celsius per second (°C/s)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Standard units match the canonical storage format, so no conversion is required
    /// for validation ranges or calculations.
    /// </para>
    /// </remarks>
    Standard,

    /// <summary>
    /// American Standard (US Customary) unit system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system uses US customary units:
    /// <list type="bullet">
    ///   <item><description><strong>Thickness:</strong> inches (in)</description></item>
    ///   <item><description><strong>Cooling Rate:</strong> degrees Fahrenheit per second (°F/s)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When this system is active:
    /// <list type="number">
    ///   <item><description>User input is validated in American Standard units</description></item>
    ///   <item><description>Valid values are converted to SI units (mm, °C/s) for storage</description></item>
    ///   <item><description>Stored SI values are converted back to American Standard for display</description></item>
    ///   <item><description>All calculations use the underlying SI values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Conversion Factors:</strong>
    /// <list type="bullet">
    ///   <item><description>1 inch = 25.4 mm</description></item>
    ///   <item><description>°F/s = °C/s × (9/5)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    AmericanStandard
}