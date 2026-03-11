using Castara.Domain.Casting;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Defines a strategy for estimating cast iron properties based on a specific casting process family.
/// </summary>
/// <remarks>
/// <para>
/// This interface implements the **Strategy Pattern**, allowing different estimation algorithms
/// to be applied based on the casting process family (e.g., Green Sand, No-Bake, Shell Mold).
/// Each strategy encapsulates process-specific tuning, risk assessment logic, and property
/// calculation adjustments.
/// </para>
/// <para>
/// <strong>Strategy Selection:</strong> The appropriate strategy is selected at runtime based on
/// the <see cref="CastingProfileDefinition.ProcessFamily"/> of the active profile. This allows
/// the estimation system to apply different metallurgical models and correction factors for
/// different casting processes.
/// </para>
/// <para>
/// <strong>Implementation Responsibilities:</strong>
/// <list type="bullet">
///   <item><description>Apply process-specific cooling rate adjustments</description></item>
///   <item><description>Calculate graphitization tendency with process bias factors</description></item>
///   <item><description>Estimate hardness ranges considering mold thermal characteristics</description></item>
///   <item><description>Generate risk flags using process-specific thresholds</description></item>
///   <item><description>Account for typical inoculation and treatment practices</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Example Strategies:</strong>
/// <list type="bullet">
///   <item><description><c>GreenSandEstimatorStrategy</c> - Moderate cooling, flexible feeding</description></item>
///   <item><description><c>NoBakeEstimatorStrategy</c> - Faster cooling, rigid mold constraints</description></item>
///   <item><description><c>ShellMoldEstimatorStrategy</c> - Rapid cooling, precise dimensions</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICastingEstimatorStrategy
{
    /// <summary>
    /// Gets the casting process family identifier that this strategy supports.
    /// </summary>
    /// <value>
    /// A string identifier matching <see cref="CastingProfileDefinition.ProcessFamily"/>
    /// (e.g., "GreenSand", "NoBake", "ShellMold", "Investment").
    /// </value>
    /// <remarks>
    /// <para>
    /// This property is used by the estimation system to select the appropriate strategy
    /// based on the active casting profile. The value should exactly match the
    /// <c>ProcessFamily</c> field in the corresponding profile definitions.
    /// </para>
    /// <para>
    /// <strong>Matching Examples:</strong>
    /// <list type="bullet">
    ///   <item><description>Strategy returns "GreenSand" → matches profiles with ProcessFamily = "GreenSand"</description></item>
    ///   <item><description>Strategy returns "NoBake" → matches profiles with ProcessFamily = "NoBake"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    string SupportedProfileFamily { get; }

    /// <summary>
    /// Estimates cast iron mechanical properties and identifies risk conditions using
    /// process-specific algorithms and thresholds.
    /// </summary>
    /// <param name="castIronInputs">
    /// The cast iron composition and section parameters to analyze.
    /// Contains validated chemical composition (C, Si, Mn, P, S) and section profile data.
    /// </param>
    /// <param name="section">
    /// The casting section characteristics including thickness and cooling rate.
    /// Used to adjust property predictions based on section size effects.
    /// </param>
    /// <param name="profile">
    /// The casting profile definition containing process-specific constraints, biases,
    /// and risk thresholds. Provides tuning parameters for this estimation strategy.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing:
    /// <list type="bullet">
    ///   <item><description>Carbon equivalent and graphitization score</description></item>
    ///   <item><description>Estimated hardness range (min/max HB)</description></item>
    ///   <item><description>Cooling and thickness factors</description></item>
    ///   <item><description>Process-specific risk flags (chill, shrinkage, hardness warnings)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Estimation Process:</strong>
    /// <list type="number">
    ///   <item><description>Calculate base carbon equivalent from composition</description></item>
    ///   <item><description>Apply process-specific graphitization bias from profile</description></item>
    ///   <item><description>Adjust for section thickness and cooling rate effects</description></item>
    ///   <item><description>Estimate hardness range using process cooling severity</description></item>
    ///   <item><description>Evaluate risk conditions against profile thresholds</description></item>
    ///   <item><description>Generate appropriate risk flags with severity levels</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Process-Specific Adjustments:</strong> Implementations should use the profile's
    /// tuning parameters (<see cref="CastingProfileDefinition.GraphitizationBias"/>,
    /// <see cref="CastingProfileDefinition.CoolingSeverityFactor"/>, etc.) to adapt calculations
    /// for the specific casting method's thermal and metallurgical characteristics.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the profile's <see cref="CastingProfileDefinition.ProcessFamily"/>
    /// does not match this strategy's <see cref="SupportedProfileFamily"/>.
    /// </exception>
    CastIronEstimate Estimate(
        CastIronInputs castIronInputs,
        SectionProfile section,
        CastingProfileDefinition profile);
}