using System;
using Castara.Domain.Exceptions;

namespace Castara.Domain.Casting;

/// <summary>
/// Defines a complete casting profile configuration including composition constraints,
/// process parameters, and risk assessment thresholds for a specific iron type and casting method.
/// </summary>
/// <remarks>
/// <para>
/// A casting profile encapsulates all the metadata needed to constrain and validate
/// cast iron composition inputs and estimation parameters for a specific combination of:
/// <list type="bullet">
///   <item><description>Iron type (gray, ductile, malleable, etc.)</description></item>
///   <item><description>Process family (green sand, no-bake, shell mold, etc.)</description></item>
///   <item><description>Typical section characteristics</description></item>
/// </list>
/// </para>
/// <para>
/// Profiles are typically loaded from configuration (e.g., JSON) and used to:
/// <list type="bullet">
///   <item><description>Constrain input ranges for composition and section parameters</description></item>
///   <item><description>Provide process-specific defaults</description></item>
///   <item><description>Tune risk assessment algorithms for the specific casting method</description></item>
///   <item><description>Adjust hardness and property expectations</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Example Use Cases:</strong>
/// <list type="bullet">
///   <item><description>Green Sand Gray Iron - Class 30 (general purpose)</description></item>
///   <item><description>No-Bake Gray Iron - High Production (thin sections)</description></item>
///   <item><description>Shell Mold Gray Iron - Precision Casting (tight tolerances)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Id">
/// Unique identifier for the profile (e.g., "GS_GRAY_30", "NB_GRAY_HIGHPROD").
/// Used for profile selection and persistence.
/// </param>
/// <param name="DisplayName">
/// Human-readable name for UI display (e.g., "Green Sand Gray Iron - Class 30").
/// </param>
/// <param name="ProcessFamily">
/// The casting process family (e.g., "GreenSand", "NoBake", "ShellMold", "Investment").
/// Used for process-specific tuning and filtering.
/// </param>
/// <param name="IronType">
/// The type of cast iron (e.g., "Gray", "Ductile", "Malleable", "Compacted").
/// Determines applicable metallurgical models and property calculations.
/// </param>
/// <param name="DefaultSectionThicknessIn">
/// Default section thickness in inches for this casting profile.
/// Represents typical wall thickness for this process/application.
/// </param>
/// <param name="CarbonMin">
/// Minimum allowable carbon content in weight percent (wt%).
/// Lower bound for composition validation.
/// </param>
/// <param name="CarbonMax">
/// Maximum allowable carbon content in weight percent (wt%).
/// Upper bound for composition validation.
/// </param>
/// <param name="SiliconMin">
/// Minimum allowable silicon content in weight percent (wt%).
/// Lower bound for composition validation.
/// </param>
/// <param name="SiliconMax">
/// Maximum allowable silicon content in weight percent (wt%).
/// Upper bound for composition validation.
/// </param>
/// <param name="ManganeseMin">
/// Minimum allowable manganese content in weight percent (wt%).
/// Lower bound for composition validation.
/// </param>
/// <param name="ManganeseMax">
/// Maximum allowable manganese content in weight percent (wt%).
/// Upper bound for composition validation.
/// </param>
/// <param name="PhosphorusMin">
/// Minimum allowable phosphorus content in weight percent (wt%).
/// Lower bound for composition validation.
/// </param>
/// <param name="PhosphorusMax">
/// Maximum allowable phosphorus content in weight percent (wt%).
/// Upper bound for composition validation.
/// </param>
/// <param name="SulfurMin">
/// Minimum allowable sulfur content in weight percent (wt%).
/// Lower bound for composition validation.
/// </param>
/// <param name="SulfurMax">
/// Maximum allowable sulfur content in weight percent (wt%).
/// Upper bound for composition validation.
/// </param>
/// <param name="PreferredCarbonEquivalentMin">
/// Minimum preferred carbon equivalent (CE) value for optimal properties.
/// Values below this may trigger warnings but are not invalid.
/// Used for risk assessment and guidance.
/// </param>
/// <param name="PreferredCarbonEquivalentMax">
/// Maximum preferred carbon equivalent (CE) value for optimal properties.
/// Values above this may trigger warnings but are not invalid.
/// Used for risk assessment and guidance.
/// </param>
/// <param name="GraphitizationBias">
/// Process-specific bias factor for graphitization tendency calculation.
/// Adjusts for process variables like cooling rate, inoculation practice, etc.
/// Positive values increase graphitization tendency, negative values decrease it.
/// </param>
/// <param name="CoolingSeverityFactor">
/// Multiplier for cooling rate effects on microstructure and properties.
/// Accounts for process-specific cooling characteristics (mold type, thermal mass, etc.).
/// Higher values indicate more severe cooling (e.g., metal molds vs. sand).
/// </param>
/// <param name="ChillRiskCeiling">
/// Carbon equivalent threshold above which chill (white iron) risk is considered low.
/// Process-specific due to mold thermal characteristics and cooling rates.
/// Higher CE generally reduces chill risk.
/// </param>
/// <param name="ShrinkageRiskFloor">
/// Carbon equivalent threshold below which shrinkage/porosity risk increases.
/// Lower CE compositions have higher liquid shrinkage and feeding requirements.
/// Process-specific based on feeding effectiveness and mold rigidity.
/// </param>
/// <param name="HardnessWarningMinBhn">
/// Minimum hardness (Brinell) threshold for generating low-hardness warnings.
/// Values below this may indicate soft spots, inadequate cooling, or composition issues.
/// Process-specific based on typical achievable properties.
/// </param>
/// <param name="HardnessWarningMaxBhn">
/// Maximum hardness (Brinell) threshold for generating high-hardness warnings.
/// Values above this may indicate chill, rapid cooling, or machinability concerns.
/// Process-specific based on typical achievable properties and application requirements.
/// </param>
public sealed record CastingProfileDefinition(
    string Id,
    string DisplayName,
    string ProcessFamily,
    string IronType,
    double DefaultSectionThicknessIn,
    double CarbonMin,
    double CarbonMax,
    double SiliconMin,
    double SiliconMax,
    double ManganeseMin,
    double ManganeseMax,
    double PhosphorusMin,
    double PhosphorusMax,
    double SulfurMin,
    double SulfurMax,
    double PreferredCarbonEquivalentMin,
    double PreferredCarbonEquivalentMax,
    double GraphitizationBias,
    double CoolingSeverityFactor,
    double ChillRiskCeiling,
    double ShrinkageRiskFloor,
    double HardnessWarningMinBhn,
    double HardnessWarningMaxBhn)
{
    // ============================================================
    // Validation Methods
    // ============================================================

    /// <summary>
    /// Validates that all composition ranges are properly configured (min &lt;= max).
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when any min/max pair is invalid (min &gt; max) or required values are out of acceptable bounds.
    /// </exception>
    /// <remarks>
    /// This method should be called after loading profile definitions from configuration
    /// to ensure data integrity before use in validation and estimation.
    /// </remarks>
    public void ValidateRanges()
    {
        if (CarbonMin > CarbonMax)
            throw new DomainException($"Profile '{Id}': CarbonMin ({CarbonMin}) > CarbonMax ({CarbonMax})");

        if (SiliconMin > SiliconMax)
            throw new DomainException($"Profile '{Id}': SiliconMin ({SiliconMin}) > SiliconMax ({SiliconMax})");

        if (ManganeseMin > ManganeseMax)
            throw new DomainException($"Profile '{Id}': ManganeseMin ({ManganeseMin}) > ManganeseMax ({ManganeseMax})");

        if (PhosphorusMin > PhosphorusMax)
            throw new DomainException($"Profile '{Id}': PhosphorusMin ({PhosphorusMin}) > PhosphorusMax ({PhosphorusMax})");

        if (SulfurMin > SulfurMax)
            throw new DomainException($"Profile '{Id}': SulfurMin ({SulfurMin}) > SulfurMax ({SulfurMax})");

        if (PreferredCarbonEquivalentMin > PreferredCarbonEquivalentMax)
            throw new DomainException($"Profile '{Id}': PreferredCarbonEquivalentMin ({PreferredCarbonEquivalentMin}) > PreferredCarbonEquivalentMax ({PreferredCarbonEquivalentMax})");

        if (HardnessWarningMinBhn > HardnessWarningMaxBhn)
            throw new DomainException($"Profile '{Id}': HardnessWarningMinBhn ({HardnessWarningMinBhn}) > HardnessWarningMaxBhn ({HardnessWarningMaxBhn})");

        if (DefaultSectionThicknessIn <= 0)
            throw new DomainException($"Profile '{Id}': DefaultSectionThicknessIn ({DefaultSectionThicknessIn}) must be > 0");

        if (CoolingSeverityFactor < 0)
            throw new DomainException($"Profile '{Id}': CoolingSeverityFactor ({CoolingSeverityFactor}) must be >= 0");
    }

    // ============================================================
    // Composition Validation Helpers
    // ============================================================

    /// <summary>
    /// Determines whether a carbon content value is within the allowable range for this profile.
    /// </summary>
    /// <param name="carbon">The carbon content in weight percent to check.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    public bool IsValidCarbon(double carbon) => carbon >= CarbonMin && carbon <= CarbonMax;

    /// <summary>
    /// Determines whether a silicon content value is within the allowable range for this profile.
    /// </summary>
    /// <param name="silicon">The silicon content in weight percent to check.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    public bool IsValidSilicon(double silicon) => silicon >= SiliconMin && silicon <= SiliconMax;

    /// <summary>
    /// Determines whether a manganese content value is within the allowable range for this profile.
    /// </summary>
    /// <param name="manganese">The manganese content in weight percent to check.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    public bool IsValidManganese(double manganese) => manganese >= ManganeseMin && manganese <= ManganeseMax;

    /// <summary>
    /// Determines whether a phosphorus content value is within the allowable range for this profile.
    /// </summary>
    /// <param name="phosphorus">The phosphorus content in weight percent to check.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    public bool IsValidPhosphorus(double phosphorus) => phosphorus >= PhosphorusMin && phosphorus <= PhosphorusMax;

    /// <summary>
    /// Determines whether a sulfur content value is within the allowable range for this profile.
    /// </summary>
    /// <param name="sulfur">The sulfur content in weight percent to check.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    public bool IsValidSulfur(double sulfur) => sulfur >= SulfurMin && sulfur <= SulfurMax;

    // ============================================================
    // Carbon Equivalent Assessment
    // ============================================================

    /// <summary>
    /// Determines whether a carbon equivalent value is within the preferred optimal range.
    /// </summary>
    /// <param name="carbonEquivalent">The carbon equivalent value to check.</param>
    /// <returns>
    /// <c>true</c> if CE is within the preferred range; <c>false</c> if it may trigger warnings.
    /// </returns>
    /// <remarks>
    /// Values outside the preferred range are not invalid, but may result in risk flags
    /// being generated during estimation (e.g., chill risk, shrinkage risk).
    /// </remarks>
    public bool IsOptimalCarbonEquivalent(double carbonEquivalent)
        => carbonEquivalent >= PreferredCarbonEquivalentMin && carbonEquivalent <= PreferredCarbonEquivalentMax;

    /// <summary>
    /// Determines whether a carbon equivalent value indicates low chill risk for this process.
    /// </summary>
    /// <param name="carbonEquivalent">The carbon equivalent value to check.</param>
    /// <returns>
    /// <c>true</c> if CE is above the chill risk ceiling (low risk); otherwise, <c>false</c>.
    /// </returns>
    public bool HasLowChillRisk(double carbonEquivalent)
        => carbonEquivalent >= ChillRiskCeiling;

    /// <summary>
    /// Determines whether a carbon equivalent value indicates elevated shrinkage risk for this process.
    /// </summary>
    /// <param name="carbonEquivalent">The carbon equivalent value to check.</param>
    /// <returns>
    /// <c>true</c> if CE is below the shrinkage risk floor (elevated risk); otherwise, <c>false</c>.
    /// </returns>
    public bool HasElevatedShrinkageRisk(double carbonEquivalent)
        => carbonEquivalent <= ShrinkageRiskFloor;

    // ============================================================
    // Hardness Assessment
    // ============================================================

    /// <summary>
    /// Determines whether a hardness value is within the acceptable range for this profile.
    /// </summary>
    /// <param name="hardnessHb">The Brinell hardness value to check.</param>
    /// <returns>
    /// <c>true</c> if hardness is within acceptable limits; <c>false</c> if it may trigger warnings.
    /// </returns>
    public bool IsAcceptableHardness(double hardnessHb)
        => hardnessHb >= HardnessWarningMinBhn && hardnessHb <= HardnessWarningMaxBhn;

    /// <summary>
    /// Determines whether a hardness value is below the minimum acceptable threshold.
    /// </summary>
    /// <param name="hardnessHb">The Brinell hardness value to check.</param>
    /// <returns>
    /// <c>true</c> if hardness is below minimum (may indicate soft spots); otherwise, <c>false</c>.
    /// </returns>
    public bool IsTooSoft(double hardnessHb)
        => hardnessHb < HardnessWarningMinBhn;

    /// <summary>
    /// Determines whether a hardness value is above the maximum acceptable threshold.
    /// </summary>
    /// <param name="hardnessHb">The Brinell hardness value to check.</param>
    /// <returns>
    /// <c>true</c> if hardness is above maximum (may indicate chill or machinability issues); otherwise, <c>false</c>.
    /// </returns>
    public bool IsTooHard(double hardnessHb)
        => hardnessHb > HardnessWarningMaxBhn;
}