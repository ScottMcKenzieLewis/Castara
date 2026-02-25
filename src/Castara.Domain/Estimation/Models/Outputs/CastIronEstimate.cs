namespace Castara.Domain.Estimation.Models.Outputs;

/// <summary>
/// Represents the complete results of a cast iron property estimation, including calculated
/// metallurgical properties, derived factors, and identified risk conditions.
/// </summary>
/// <param name="CarbonEquivalent">
/// The calculated carbon equivalent (CE) value using the formula: CE = %C + (%Si + %P) / 3.
/// Typical values range from 3.0 to 4.5 for gray iron, with higher values promoting graphitization.
/// </param>
/// <param name="GraphitizationScore">
/// A normalized score (0-1 scale) indicating the tendency for graphite formation versus carbide formation.
/// Higher scores (&gt;0.6) indicate strong graphitization (softer, more machinable structure).
/// Lower scores (&lt;0.4) indicate carbide tendency (harder, potential chill risk).
/// </param>
/// <param name="EstimatedHardness">
/// The predicted Brinell hardness range (HB) for the casting, accounting for natural variation
/// in microstructure and properties across the section.
/// </param>
/// <param name="CoolingFactor">
/// A normalized factor representing the cooling rate's influence on microstructure formation.
/// Derived from the input cooling rate (°C/s) using logarithmic interpolation.
/// Negative values indicate slow cooling; positive values indicate fast cooling.
/// </param>
/// <param name="ThicknessFactor">
/// A normalized factor representing the section thickness deviation from the reference thickness.
/// Positive values indicate thinner sections (faster cooling); negative values indicate thicker sections.
/// </param>
/// <param name="Flags">
/// A collection of risk flags identifying potential concerns such as chill risk, shrinkage sensitivity,
/// or machinability issues. Flags are prioritized by severity (High, Medium, Low).
/// </param>
/// <remarks>
/// <para>
/// This estimation combines multiple metallurgical calculations to provide a comprehensive
/// assessment of expected cast iron properties. The results are derived from:
/// <list type="bullet">
///   <item><description>Standard carbon equivalent formula</description></item>
///   <item><description>Empirical graphitization models based on composition and cooling</description></item>
///   <item><description>Hardness correlations with microstructure predictions</description></item>
///   <item><description>Risk assessment algorithms for common casting defects</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Interpretation Guidelines:</strong>
/// </para>
/// <para>
/// <strong>Carbon Equivalent:</strong> Values around 3.6-4.0 typically produce good gray iron.
/// Lower values (&lt;3.4) increase shrinkage and chill risk. Higher values (&gt;4.3) may cause
/// excessive graphitization or flotation.
/// </para>
/// <para>
/// <strong>Graphitization Score:</strong> Values of 0.5-0.7 represent ideal conditions for
/// gray iron with good machinability and moderate hardness. Scores below 0.4 suggest reviewing
/// composition or process parameters to avoid chill formation.
/// </para>
/// <para>
/// <strong>Risk Flags:</strong> High-severity flags require immediate attention and may necessitate
/// composition adjustments, modified gating/risering designs, or process parameter changes.
/// </para>
/// <para>
/// <strong>Important:</strong> These estimates are based on empirical models and industry standards.
/// Always validate predictions with laboratory testing and metallographic analysis for critical
/// applications. Consult qualified metallurgists for interpretation and process optimization.
/// </para>
/// </remarks>
/// <example>
/// Example of a typical gray iron estimation result:
/// <code>
/// var estimate = new CastIronEstimate(
///     CarbonEquivalent: 3.814,
///     GraphitizationScore: 0.623,
///     EstimatedHardness: new HardnessRange(MinHB: 205, MaxHB: 235),
///     CoolingFactor: 0.000,
///     ThicknessFactor: -0.063,
///     Flags: new List&lt;RiskFlag&gt;
///     {
///         new RiskFlag("CHILL_RISK", "Chill Risk", RiskSeverity.Low, "Low chill risk under current conditions."),
///         new RiskFlag("SHRINK_RISK", "Shrink/Porosity Risk", RiskSeverity.Low, "Low feeding sensitivity expected."),
///         new RiskFlag("MACHINABILITY", "Machinability Concern", RiskSeverity.Low, "Machinability likely good (more graphitic).")
///     }
/// );
/// </code>
/// </example>
public sealed record CastIronEstimate(
    double CarbonEquivalent,
    double GraphitizationScore,
    HardnessRange EstimatedHardness,
    double CoolingFactor,
    double ThicknessFactor,
    IReadOnlyList<RiskFlag> Flags);