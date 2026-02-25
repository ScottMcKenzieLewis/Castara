namespace Castara.Domain.Estimation.Models.Outputs;

/// <summary>
/// Represents an estimated hardness range for cast iron in Brinell Hardness (HB) units.
/// </summary>
/// <param name="MinHB">
/// The minimum expected Brinell hardness value in the range.
/// </param>
/// <param name="MaxHB">
/// The maximum expected Brinell hardness value in the range.
/// </param>
/// <remarks>
/// <para>
/// Cast iron hardness varies due to microstructural differences across the casting,
/// localized composition variations, and cooling rate gradients. This range represents
/// the expected hardness span rather than a single point value, providing a more
/// realistic estimation that accounts for natural variation.
/// </para>
/// <para>
/// Typical hardness ranges for common cast iron types:
/// <list type="table">
///   <listheader>
///     <term>Iron Type</term>
///     <description>Typical Hardness (HB)</description>
///   </listheader>
///   <item>
///     <term>Soft Gray Iron</term>
///     <description>140-180 HB</description>
///   </item>
///   <item>
///     <term>Medium Gray Iron</term>
///     <description>180-240 HB</description>
///   </item>
///   <item>
///     <term>Hard Gray Iron</term>
///     <description>240-300 HB</description>
///   </item>
///   <item>
///     <term>White Iron (Chilled)</term>
///     <description>300-600+ HB</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The hardness range is influenced by:
/// <list type="bullet">
///   <item><description>Graphitization score (lower graphitization = higher hardness)</description></item>
///   <item><description>Cooling rate (faster cooling = higher hardness)</description></item>
///   <item><description>Section thickness (thinner sections = higher hardness)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example hardness range for typical gray iron:
/// <code>
/// var hardness = new HardnessRange(MinHB: 205, MaxHB: 235);
/// Console.WriteLine(hardness); // Output: "205-235 HB"
/// </code>
/// </example>
public readonly record struct HardnessRange(int MinHB, int MaxHB)
{
    /// <summary>
    /// Returns a string representation of the hardness range in the format "Min-Max HB".
    /// </summary>
    /// <returns>
    /// A formatted string showing the hardness range with Brinell Hardness units
    /// (e.g., "205-235 HB").
    /// </returns>
    /// <remarks>
    /// This format is suitable for display in user interfaces and reports, clearly
    /// indicating the range span and measurement units.
    /// </remarks>
    public override string ToString() => $"{MinHB}-{MaxHB} HB";
}