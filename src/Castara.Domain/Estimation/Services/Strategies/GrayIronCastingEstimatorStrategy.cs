using System;
using System.Collections.Generic;
using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Validation;

namespace Castara.Domain.Estimation.Services.Strategies;

/// <summary>
/// Estimation strategy for gray cast iron using a unified metallurgical model with
/// profile-driven tuning for different casting processes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Decision: One Strategy for All Gray Iron Profiles</strong>
/// </para>
/// <para>
/// Multiple gray iron profiles intentionally share this strategy. That is a modeling choice:
/// green sand, resin sand, shell mold, and heavy-section variants are currently treated as
/// parameter changes inside the same gray iron framework rather than as entirely separate algorithms.
/// </para>
/// <para>
/// This keeps the model coherent. We reserve separate strategies for genuinely different
/// metallurgical regimes, such as a future ductile iron estimator. Process-family differences
/// (green sand vs. shell mold) are expressed through tuning values on the profile:
/// <list type="bullet">
///   <item><description><see cref="CastingProfileDefinition.CoolingSeverityFactor"/> - How aggressively the process cools</description></item>
///   <item><description><see cref="CastingProfileDefinition.GraphitizationBias"/> - Process tendency toward graphitic vs. carbide structures</description></item>
///   <item><description><see cref="CastingProfileDefinition.ChillRiskCeiling"/> - Process-specific CE threshold for chill risk</description></item>
///   <item><description><see cref="CastingProfileDefinition.ShrinkageRiskFloor"/> - Process-specific CE threshold for feeding sensitivity</description></item>
///   <item><description><see cref="CastingProfileDefinition.HardnessWarningMaxBhn"/> - Acceptable hardness range for the process</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Metallurgical Model:</strong>
/// </para>
/// <para>
/// This strategy implements the standard gray iron estimation model:
/// <list type="number">
///   <item><description>Calculate carbon equivalent: CE = C + (Si + P) / 3</description></item>
///   <item><description>Compute cooling factor from process-adjusted cooling rate (log-space interpolation)</description></item>
///   <item><description>Compute thickness factor relative to reference section</description></item>
///   <item><description>Calculate graphitization score (0-1 scale) with profile bias scaling</description></item>
///   <item><description>Predict hardness range from graphitization and solidification factors</description></item>
///   <item><description>Generate risk flags using profile-specific thresholds and severity scaling</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Profile Integration:</strong>
/// </para>
/// <para>
/// The profile affects calculations at key points:
/// <list type="bullet">
///   <item><description>Cooling rate is scaled by <c>CoolingSeverityFactor</c> before normalization</description></item>
///   <item><description>Graphitization score is scaled by <c>GraphitizationBias</c> as a multiplier</description></item>
///   <item><description>Risk flags evaluate against profile-specific thresholds (<c>ChillRiskCeiling</c>, <c>ShrinkageRiskFloor</c>)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GrayIronCastingEstimatorStrategy : ICastingEstimatorStrategy
{
    // ============================================================
    // Public Methods - Strategy Interface
    // ============================================================

    /// <summary>
    /// Determines whether this strategy can handle the specified casting profile.
    /// </summary>
    /// <param name="profile">The casting profile to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the profile's iron type is "GrayIron" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="profile"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Matching Strategy: Iron Type, Not Process Family</strong>
    /// </para>
    /// <para>
    /// We match by iron type because the current behavioral split is metallurgical first.
    /// Process-family differences still matter, but today they are expressed through
    /// tuning values on the profile rather than through wholly different equations.
    /// </para>
    /// <para>
    /// This means profiles with <c>IronType = "GrayIron"</c> will use this strategy
    /// regardless of their <c>ProcessFamily</c> (green sand, no-bake, shell mold, etc.).
    /// The process family differences are captured through the profile's tuning parameters.
    /// </para>
    /// </remarks>
    public bool CanHandle(CastingProfileDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // We match by iron type because the current behavioral split is metallurgical first.
        // Process-family differences still matter, but today they are expressed through
        // tuning values on the profile rather than through wholly different equations.
        return string.Equals(profile.IronType, "GrayIron", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Estimates gray iron mechanical properties and risk conditions using the unified
    /// gray iron metallurgical model with profile-specific tuning.
    /// </summary>
    /// <param name="inputs">
    /// The validated cast iron composition and section parameters.
    /// </param>
    /// <param name="profile">
    /// The casting profile providing tuning parameters and risk thresholds.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing carbon equivalent, graphitization score,
    /// hardness range, adjustment factors, and risk flags.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputs"/>, <paramref name="profile"/>,
    /// <paramref name="inputs"/>.Composition, or <paramref name="inputs"/>.Section is null.
    /// </exception>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>Profile ranges are invalid (min > max, negative values where positive required)</description></item>
    ///   <item><description>Composition values violate global sanity bounds</description></item>
    ///   <item><description>Section parameters are physically nonsensical</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Validation Philosophy:</strong>
    /// </para>
    /// <para>
    /// We validate the profile at the domain boundary because a bad profile is not merely
    /// "bad configuration"; it would invalidate the assumptions behind every downstream calculation.
    /// Profile-specific range enforcement can happen in the application/UI layer as well,
    /// but the domain should still reject values that are globally nonsensical.
    /// </para>
    /// <para>
    /// <strong>Calculation Flow:</strong>
    /// </para>
    /// <list type="number">
    ///   <item><description>Validate profile ranges and input sanity</description></item>
    ///   <item><description>Compute carbon equivalent (CE = C + (Si + P) / 3)</description></item>
    ///   <item><description>Apply profile's cooling severity factor to raw cooling rate</description></item>
    ///   <item><description>Compute cooling factor via log-space interpolation</description></item>
    ///   <item><description>Compute thickness factor relative to reference section</description></item>
    ///   <item><description>Calculate graphitization score with profile bias scaling</description></item>
    ///   <item><description>Predict hardness range from graphitization and factors</description></item>
    ///   <item><description>Generate risk flags using profile thresholds</description></item>
    /// </list>
    /// </remarks>
    public CastIronEstimate Estimate(CastIronInputs inputs, CastingProfileDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(inputs.Composition);
        ArgumentNullException.ThrowIfNull(inputs.Section);

        // We validate the profile at the domain boundary because a bad profile is not merely
        // "bad configuration"; it would invalidate the assumptions behind every downstream calculation.
        profile.Validate();

        var composition = inputs.Composition;
        var section = inputs.Section;

        // These guards still protect the base physical sanity of the inputs.
        // Profile-specific range enforcement can happen in the application/UI layer as well,
        // but the domain should still reject values that are globally nonsensical.
        CompositionGuards.Validate(composition);
        SectionGuards.Validate(section);

        var ce = ComputeCarbonEquivalent(composition);

        // The profile changes the meaning of the same physical cooling rate.
        // We scale the raw cooling rate before normalization because the process family
        // affects how aggressively a section "behaves" thermally, not just how we label it.
        var effectiveCoolingRate = section.CoolingRateCPerSec * profile.CoolingSeverityFactor;
        var coolingFactor = ComputeCoolingFactor(effectiveCoolingRate);

        var thicknessFactor = ComputeThicknessFactor(section.ThicknessMm);

        // Graphitization bias is treated as a multiplier rather than a raw additive bump.
        // That choice keeps the base score recognizable while still allowing profiles to nudge
        // the result up or down without dominating it.
        var graphScore = ComputeGraphitizationScore(
            ce,
            coolingFactor,
            thicknessFactor,
            profile);

        var hardness = ComputeHardness(
            graphScore,
            coolingFactor,
            thicknessFactor);

        var flags = new List<RiskFlag>
        {
            BuildChillRisk(ce, graphScore, coolingFactor, thicknessFactor, profile),
            BuildShrinkRisk(ce, thicknessFactor, composition.Manganese, profile),
            BuildMachinabilityRisk(graphScore, hardness, profile)
        };

        return new CastIronEstimate(
            CarbonEquivalent: Math.Round(ce, 3),
            GraphitizationScore: Math.Round(graphScore, 3),
            EstimatedHardness: hardness,
            CoolingFactor: Math.Round(coolingFactor, 3),
            ThicknessFactor: Math.Round(thicknessFactor, 3),
            Flags: flags);
    }

    // ============================================================
    // Private Methods - Core Calculations
    // ============================================================

    /// <summary>
    /// Calculates carbon equivalent using the standard gray iron formulation.
    /// </summary>
    /// <param name="composition">The cast iron chemical composition.</param>
    /// <returns>
    /// Carbon equivalent: CE = %C + (%Si + %P) / 3
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Profile Independence:</strong>
    /// </para>
    /// <para>
    /// We keep this formula profile-independent because CE is a metallurgical quantity,
    /// not a process-specific opinion. Profiles influence how CE is interpreted (via risk
    /// thresholds like <c>ChillRiskCeiling</c> and <c>ShrinkageRiskFloor</c>), not how CE
    /// is defined.
    /// </para>
    /// <para>
    /// This is the standard gray iron CE formula. Ductile iron would use a different
    /// formula (e.g., CE = C + 0.31·Si + 0.33·P), which would justify a separate strategy.
    /// </para>
    /// </remarks>
    private static double ComputeCarbonEquivalent(CastIronComposition composition)
        => composition.Carbon + (composition.Silicon + composition.Phosphorus) / 3.0;

    /// <summary>
    /// Converts a cooling rate into a normalized factor using log-space interpolation.
    /// </summary>
    /// <param name="coolingRateCPerSec">
    /// The effective cooling rate in °C/s (already scaled by profile's cooling severity factor).
    /// </param>
    /// <returns>
    /// A normalized cooling factor (typically -0.15 to +0.20) where:
    /// <list type="bullet">
    ///   <item><description>Negative values indicate slower cooling (more graphitic)</description></item>
    ///   <item><description>Positive values indicate faster cooling (more carbide tendency)</description></item>
    ///   <item><description>Zero corresponds to the reference cooling rate (1.0 °C/s)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Logarithmic Mapping Rationale:</strong>
    /// </para>
    /// <para>
    /// The log mapping is deliberate. In foundry practice, the difference between 0.1 and 1.0 °C/s
    /// is often more meaningful than a simple linear scale would imply, so we normalize in a way
    /// that better reflects how microstructure sensitivity is experienced in practice.
    /// </para>
    /// <para>
    /// <strong>Interpolation Points:</strong>
    /// <list type="bullet">
    ///   <item><description>Slow: 0.1 °C/s → factor = -0.15 (graphitic tendency)</description></item>
    ///   <item><description>Normal: 1.0 °C/s → factor = 0.00 (reference)</description></item>
    ///   <item><description>Fast: 10.0 °C/s → factor = +0.20 (carbide tendency)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Values are clamped to 0.02–50.0 °C/s to prevent extrapolation beyond reasonable bounds.
    /// </para>
    /// </remarks>
    private static double ComputeCoolingFactor(double coolingRateCPerSec)
    {
        var rate = Math.Clamp(coolingRateCPerSec, 0.02, 50.0);

        const double rSlow = 0.1;
        const double fSlow = -0.15;

        const double rNorm = 1.0;
        const double fNorm = 0.00;

        const double rFast = 10.0;
        const double fFast = 0.20;

        var x = Math.Log10(rate);

        var xSlow = Math.Log10(rSlow);
        var xNorm = Math.Log10(rNorm);
        var xFast = Math.Log10(rFast);

        return x <= xNorm
            ? Lerp(xSlow, fSlow, xNorm, fNorm, x)
            : Lerp(xNorm, fNorm, xFast, fFast, x);
    }

    /// <summary>
    /// Computes the thickness factor relative to the model's reference section.
    /// </summary>
    /// <param name="thicknessMm">The section thickness in millimeters.</param>
    /// <returns>
    /// A normalized thickness factor where:
    /// <list type="bullet">
    ///   <item><description>Positive values indicate thinner sections (faster cooling, less graphitic)</description></item>
    ///   <item><description>Negative values indicate thicker sections (slower cooling, more graphitic)</description></item>
    ///   <item><description>Zero corresponds to the reference thickness (25 mm)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Separation from Cooling Rate:</strong>
    /// </para>
    /// <para>
    /// Thickness remains a separate factor from cooling rate because the two influence
    /// solidification differently: one is a process condition (how fast heat is extracted),
    /// the other is a geometric constraint (how far heat must travel). Keeping them separate
    /// preserves room for future calibration and better captures the physics.
    /// </para>
    /// <para>
    /// <strong>Calculation:</strong>
    /// Factor = (ReferenceMm - ActualMm) / ScaleMm
    /// <list type="bullet">
    ///   <item><description>Reference: 25 mm (typical medium section)</description></item>
    ///   <item><description>Scale: 50 mm (normalization range)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static double ComputeThicknessFactor(double thicknessMm)
        => (CastIronEstimationConstants.ThicknessPivotMm - thicknessMm)
           / CastIronEstimationConstants.ThicknessScale;

    /// <summary>
    /// Computes the graphitization score indicating tendency toward graphitic structure.
    /// </summary>
    /// <param name="ce">Carbon equivalent value.</param>
    /// <param name="coolingFactor">Normalized cooling rate factor.</param>
    /// <param name="thicknessFactor">Normalized thickness factor.</param>
    /// <param name="profile">Casting profile providing graphitization bias scaling.</param>
    /// <returns>
    /// Graphitization score on 0-1 scale where:
    /// <list type="bullet">
    ///   <item><description>0.0 = Strongly carbide-favoring (white iron tendency)</description></item>
    ///   <item><description>0.5-0.6 = Balanced gray iron structure</description></item>
    ///   <item><description>1.0 = Strongly graphitic (potential over-graphitization)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Profile Scaling Strategy:</strong>
    /// </para>
    /// <para>
    /// The base model remains the same as before, but the selected profile is allowed to
    /// scale the result. That reflects the idea that process family changes the tendency
    /// toward graphitic vs. carbide-favoring behavior without redefining the underlying model.
    /// </para>
    /// <para>
    /// <strong>Why Multiplicative Bias:</strong>
    /// </para>
    /// <para>
    /// Graphitization bias is treated as a multiplier rather than a raw additive bump.
    /// That choice keeps the base score recognizable while still allowing profiles to nudge
    /// the result up or down without dominating it. A bias of 1.0 leaves the score unchanged,
    /// while 1.1 increases graphitization tendency by 10%, and 0.9 decreases it by 10%.
    /// </para>
    /// <para>
    /// <strong>Calculation:</strong>
    /// BaseScore = BaseConst + CEWeight·(CE - CEPivot) - CoolingWeight·CoolingFactor - ThicknessWeight·ThicknessFactor
    /// FinalScore = Clamp01(BaseScore · GraphitizationBias)
    /// </para>
    /// </remarks>
    private static double ComputeGraphitizationScore(
        double ce,
        double coolingFactor,
        double thicknessFactor,
        CastingProfileDefinition profile)
    {
        var baseScore =
            CastIronEstimationConstants.BaseGraphScore
            + CastIronEstimationConstants.CeWeight * (ce - CastIronEstimationConstants.CePivot)
            - CastIronEstimationConstants.CoolingWeight * coolingFactor
            - CastIronEstimationConstants.ThicknessWeight * thicknessFactor;

        return Clamp01(baseScore * profile.GraphitizationBias);
    }

    /// <summary>
    /// Estimates hardness range from graphitization score and solidification severity factors.
    /// </summary>
    /// <param name="graphScore">Graphitization score (0-1 scale).</param>
    /// <param name="coolingFactor">Normalized cooling rate factor.</param>
    /// <param name="thicknessFactor">Normalized thickness factor.</param>
    /// <returns>
    /// A <see cref="HardnessRange"/> with min/max Brinell hardness values,
    /// clamped to physically reasonable bounds (100-400 HB).
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Profile-Light Approach:</strong>
    /// </para>
    /// <para>
    /// We intentionally keep hardness calculation profile-light for now.
    /// The selected profile should influence how hardness is <em>interpreted</em>
    /// (via <see cref="CastingProfileDefinition.HardnessWarningMinBhn"/> and
    /// <see cref="CastingProfileDefinition.HardnessWarningMaxBhn"/>) before it
    /// fully redefines how hardness is <em>predicted</em>.
    /// </para>
    /// <para>
    /// <strong>Calculation:</strong>
    /// <list type="bullet">
    ///   <item><description>Base hardness: 205 HB (typical Class 30 gray iron)</description></item>
    ///   <item><description>Adjusted by deviation from reference graphitization (0.55)</description></item>
    ///   <item><description>Increased by faster cooling and thinner sections</description></item>
    ///   <item><description>Range spread: ±15 HB around center value</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Future enhancement could include profile-specific hardness models for processes
    /// with fundamentally different hardness development (e.g., quenched gray iron).
    /// </para>
    /// </remarks>
    private static HardnessRange ComputeHardness(
        double graphScore,
        double coolingFactor,
        double thicknessFactor)
    {
        var hbCenter =
            CastIronEstimationConstants.BaseHardnessHB
            + (int)(CastIronEstimationConstants.HardnessGraphWeight * (0.55 - graphScore))
            + (int)(CastIronEstimationConstants.HardnessCoolingWeight * coolingFactor)
            + (int)(CastIronEstimationConstants.HardnessThicknessWeight * thicknessFactor);

        var min = ClampInt(
            hbCenter - CastIronEstimationConstants.HardnessSpreadHB,
            CastIronEstimationConstants.MinHardnessHB,
            CastIronEstimationConstants.MaxHardnessHB);

        var max = ClampInt(
            hbCenter + CastIronEstimationConstants.HardnessSpreadHB,
            CastIronEstimationConstants.MinHardnessHB,
            CastIronEstimationConstants.MaxHardnessHB);

        return new HardnessRange(MinHB: min, MaxHB: max);
    }

    // ============================================================
    // Private Methods - Risk Flag Generation
    // ============================================================

    /// <summary>
    /// Builds the chill risk flag evaluating tendency toward white iron (carbide) structure.
    /// </summary>
    /// <param name="ce">Carbon equivalent value.</param>
    /// <param name="graphScore">Graphitization score (0-1 scale).</param>
    /// <param name="coolingFactor">Normalized cooling rate factor.</param>
    /// <param name="thicknessFactor">Normalized thickness factor.</param>
    /// <param name="profile">Casting profile providing chill risk ceiling threshold.</param>
    /// <returns>
    /// A <see cref="RiskFlag"/> with severity (Low/Medium/High) and descriptive message.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Multi-Factor Risk Assessment:</strong>
    /// </para>
    /// <para>
    /// Chill risk is not only a function of "hardness-like" behavior. We also look at how far CE
    /// falls below the profile's chill threshold, because the same chemistry may be acceptable
    /// in one process family and risky in another. Green sand (slow cooling) may tolerate lower
    /// CE than shell mold (fast cooling).
    /// </para>
    /// <para>
    /// <strong>Score Calculation:</strong>
    /// <list type="bullet">
    ///   <item><description>40% weight: Deviation below reference graphitization (0.55)</description></item>
    ///   <item><description>25% weight: Faster cooling tendency</description></item>
    ///   <item><description>15% weight: Thinner section effects</description></item>
    ///   <item><description>35% weight: CE deficit below profile's chill ceiling (normalized to 0.5 CE units)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Severity Mapping:</strong>
    /// Score ≥ 0.66 → High, 0.33-0.66 → Medium, &lt; 0.33 → Low
    /// </para>
    /// </remarks>
    private static RiskFlag BuildChillRisk(
        double ce,
        double graphScore,
        double coolingFactor,
        double thicknessFactor,
        CastingProfileDefinition profile)
    {
        var ceDeficit = Math.Max(0.0, profile.ChillRiskCeiling - ce);

        var score = Clamp01(
            0.40 * (0.55 - graphScore)
            + 0.25 * coolingFactor
            + 0.15 * thicknessFactor
            + 0.35 * (ceDeficit / 0.50));

        var severity = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "CHILL_RISK",
            Name: "Chill Risk",
            Severity: severity,
            Message: severity switch
            {
                RiskSeverity.High =>
                    "High risk of chilled structure under the selected profile assumptions.",
                RiskSeverity.Medium =>
                    "Moderate chill risk. Review cooling severity, section size, and CE margin.",
                _ =>
                    "Low chill risk under the selected profile assumptions."
            });
    }

    /// <summary>
    /// Builds the shrinkage/porosity risk flag evaluating feeding sensitivity.
    /// </summary>
    /// <param name="ce">Carbon equivalent value.</param>
    /// <param name="thicknessFactor">Normalized thickness factor.</param>
    /// <param name="manganese">Manganese content (wt%).</param>
    /// <param name="profile">Casting profile providing shrinkage risk floor threshold.</param>
    /// <returns>
    /// A <see cref="RiskFlag"/> with severity (Low/Medium/High) and descriptive message.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Profile-Specific Threshold:</strong>
    /// </para>
    /// <para>
    /// The previous implementation used a generic CE relation. Here we make the threshold explicit:
    /// shrinkage risk should rise when CE falls below the profile's shrinkage floor, because that is
    /// the profile's statement of what "comfortably feedable" means for that process family.
    /// </para>
    /// <para>
    /// Different processes have different feeding capabilities:
    /// <list type="bullet">
    ///   <item><description>Green sand: More flexible mold, better feeding, lower floor</description></item>
    ///   <item><description>No-bake/shell mold: Rigid mold, restricted feeding, higher floor</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Score Calculation:</strong>
    /// <list type="bullet">
    ///   <item><description>Base risk: 20%</description></item>
    ///   <item><description>40% weight: CE deficit below profile's shrinkage floor</description></item>
    ///   <item><description>25% weight: Thicker sections (harder to feed, inverted factor)</description></item>
    ///   <item><description>5% weight: Manganese content (affects fluidity)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static RiskFlag BuildShrinkRisk(
        double ce,
        double thicknessFactor,
        double manganese,
        CastingProfileDefinition profile)
    {
        var ceDeficit = Math.Max(0.0, profile.ShrinkageRiskFloor - ce);

        var score = Clamp01(
            0.20
            + 0.40 * (ceDeficit / 0.50)
            + 0.25 * (-thicknessFactor)
            + 0.05 * manganese);

        var severity = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "SHRINK_RISK",
            Name: "Shrink/Porosity Risk",
            Severity: severity,
            Message: severity switch
            {
                RiskSeverity.High =>
                    "High feeding sensitivity under the selected profile. Review risering and feeding assumptions.",
                RiskSeverity.Medium =>
                    "Moderate feeding sensitivity. Confirm gating and riser adequacy.",
                _ =>
                    "Low feeding sensitivity expected for the selected profile."
            });
    }

    /// <summary>
    /// Builds the machinability concern flag evaluating potential machining difficulties.
    /// </summary>
    /// <param name="graphScore">Graphitization score (0-1 scale).</param>
    /// <param name="hardness">Predicted hardness range.</param>
    /// <param name="profile">Casting profile providing hardness warning thresholds.</param>
    /// <returns>
    /// A <see cref="RiskFlag"/> with severity (Low/Medium/High) and descriptive message.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Machinability Drivers:</strong>
    /// </para>
    /// <para>
    /// Machinability concerns are usually felt when the structure trends hard and carbide-favoring.
    /// We therefore combine low graphitization with whether the predicted hardness center exceeds
    /// the profile's acceptable hardness band (<see cref="CastingProfileDefinition.HardnessWarningMaxBhn"/>).
    /// </para>
    /// <para>
    /// <strong>Profile Context:</strong>
    /// </para>
    /// <para>
    /// Different applications have different machinability requirements:
    /// <list type="bullet">
    ///   <item><description>Machined components: Tight hardness control, lower warning max</description></item>
    ///   <item><description>Wear applications: Higher hardness acceptable, higher warning max</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Score Calculation:</strong>
    /// <list type="bullet">
    ///   <item><description>Base: Inverse of graphitization (0.55 - graphScore)</description></item>
    ///   <item><description>20% weight: Hardness excess above profile's maximum (normalized to 30 HB)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static RiskFlag BuildMachinabilityRisk(
        double graphScore,
        HardnessRange hardness,
        CastingProfileDefinition profile)
    {
        var hardnessCenter = (hardness.MinHB + hardness.MaxHB) / 2.0;
        var hardnessExcess = Math.Max(0.0, hardnessCenter - profile.HardnessWarningMaxBhn);

        var score = Clamp01(
            0.55 - graphScore
            + 0.20 * (hardnessExcess / 30.0));

        var severity = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "MACHINABILITY",
            Name: "Machinability Concern",
            Severity: severity,
            Message: severity switch
            {
                RiskSeverity.High =>
                    "Machinability may be challenging under the selected profile assumptions.",
                RiskSeverity.Medium =>
                    "Machinability is likely acceptable, but hardness should be verified.",
                _ =>
                    "Machinability is likely good for the selected profile."
            });
    }

    // ============================================================
    // Private Methods - Utility Functions
    // ============================================================

    /// <summary>
    /// Performs linear interpolation between two points.
    /// </summary>
    /// <param name="x0">X coordinate of first point.</param>
    /// <param name="y0">Y coordinate of first point.</param>
    /// <param name="x1">X coordinate of second point.</param>
    /// <param name="y1">Y coordinate of second point.</param>
    /// <param name="x">X coordinate to interpolate at.</param>
    /// <returns>Interpolated Y value at X.</returns>
    /// <remarks>
    /// If x0 and x1 are effectively equal (within 1e-12), returns y0 to avoid division by zero.
    /// </remarks>
    private static double Lerp(double x0, double y0, double x1, double y1, double x)
    {
        if (Math.Abs(x1 - x0) < 1e-12)
        {
            return y0;
        }

        var t = (x - x0) / (x1 - x0);
        return y0 + t * (y1 - y0);
    }

    /// <summary>
    /// Converts a normalized risk score (0-1) to a severity level.
    /// </summary>
    /// <param name="score01">Risk score on 0-1 scale.</param>
    /// <returns>
    /// <see cref="RiskSeverity"/>: High (≥0.66), Medium (0.33-0.66), or Low (&lt;0.33).
    /// </returns>
    private static RiskSeverity ScoreToSeverity(double score01)
        => score01 >= 0.66 ? RiskSeverity.High
         : score01 >= 0.33 ? RiskSeverity.Medium
         : RiskSeverity.Low;

    /// <summary>
    /// Clamps a value to the range [0, 1].
    /// </summary>
    /// <param name="value">Value to clamp.</param>
    /// <returns>Clamped value between 0 and 1.</returns>
    private static double Clamp01(double value)
        => value < 0 ? 0 : (value > 1 ? 1 : value);

    /// <summary>
    /// Clamps an integer value to the specified range.
    /// </summary>
    /// <param name="value">Value to clamp.</param>
    /// <param name="min">Minimum allowable value.</param>
    /// <param name="max">Maximum allowable value.</param>
    /// <returns>Clamped value between min and max.</returns>
    private static int ClampInt(int value, int min, int max)
        => value < min ? min : (value > max ? max : value);
}