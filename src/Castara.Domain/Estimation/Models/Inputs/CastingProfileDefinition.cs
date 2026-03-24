using System;
using Castara.Domain.Exceptions;

namespace Castara.Domain.Estimation.Models.Inputs;

/// <summary>
/// Defines a complete casting profile configuration including composition constraints,
/// process parameters, and risk assessment thresholds for a specific iron type and casting method.
/// </summary>
/// <param name="Id">Stable identifier for the profile (e.g. "gray-iron-green-sand").</param>
/// <param name="DisplayName">User-facing name for the profile.</param>
/// <param name="ProcessFamily">Broad process grouping such as GreenSand, ResinSand, or ShellMold.</param>
/// <param name="IronType">Iron family handled by this profile, such as GrayIron or DuctileIron.</param>
/// <param name="DefaultSectionThicknessMm">Default reference section thickness in millimeters.</param>
/// <param name="CarbonMin">Minimum allowable carbon percentage.</param>
/// <param name="CarbonMax">Maximum allowable carbon percentage.</param>
/// <param name="SiliconMin">Minimum allowable silicon percentage.</param>
/// <param name="SiliconMax">Maximum allowable silicon percentage.</param>
/// <param name="ManganeseMin">Minimum allowable manganese percentage.</param>
/// <param name="ManganeseMax">Maximum allowable manganese percentage.</param>
/// <param name="PhosphorusMin">Minimum allowable phosphorus percentage.</param>
/// <param name="PhosphorusMax">Maximum allowable phosphorus percentage.</param>
/// <param name="SulfurMin">Minimum allowable sulfur percentage.</param>
/// <param name="SulfurMax">Maximum allowable sulfur percentage.</param>
/// <param name="PreferredCarbonEquivalentMin">Lower bound of the preferred carbon equivalent operating window.</param>
/// <param name="PreferredCarbonEquivalentMax">Upper bound of the preferred carbon equivalent operating window.</param>
/// <param name="GraphitizationBias">
/// Profile-specific bias applied to graphitization tendency. Values above 1.0 favor graphitization,
/// while values below 1.0 suppress it.
/// </param>
/// <param name="CoolingSeverityFactor">
/// Multiplier applied to cooling-related effects. Must be positive.
/// </param>
/// <param name="ChillRiskCeiling">
/// Carbon equivalent threshold above which chill risk is considered reduced.
/// </param>
/// <param name="ShrinkageRiskFloor">
/// Carbon equivalent threshold below which shrinkage/feeding sensitivity is considered elevated.
/// </param>
/// <param name="HardnessWarningMinBhn">Lower recommended hardness threshold in Brinell hardness number (BHN).</param>
/// <param name="HardnessWarningMaxBhn">Upper recommended hardness threshold in Brinell hardness number (BHN).</param>
public sealed record CastingProfileDefinition(
    string Id,
    string DisplayName,
    string ProcessFamily,
    string IronType,
    double DefaultSectionThicknessMm,
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
    /// <summary>
    /// Validates profile configuration invariants and throws when configuration is invalid.
    /// Intended to be called after loading profiles from configuration.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the profile is invalid.</exception>
    public void Validate()
    {
        RequireNonBlank(Id, nameof(Id));
        RequireNonBlank(DisplayName, nameof(DisplayName));
        RequireNonBlank(ProcessFamily, nameof(ProcessFamily));
        RequireNonBlank(IronType, nameof(IronType));

        RequirePositive(DefaultSectionThicknessMm, nameof(DefaultSectionThicknessMm));
        RequirePositive(CoolingSeverityFactor, nameof(CoolingSeverityFactor));
        RequirePositive(GraphitizationBias, nameof(GraphitizationBias));

        ValidateRange("Carbon", CarbonMin, CarbonMax);
        ValidateRange("Silicon", SiliconMin, SiliconMax);
        ValidateRange("Manganese", ManganeseMin, ManganeseMax);
        ValidateRange("Phosphorus", PhosphorusMin, PhosphorusMax);
        ValidateRange("Sulfur", SulfurMin, SulfurMax);

        ValidateNonNegative(nameof(PreferredCarbonEquivalentMin), PreferredCarbonEquivalentMin);
        ValidateNonNegative(nameof(PreferredCarbonEquivalentMax), PreferredCarbonEquivalentMax);
        ValidateOrdered(
            nameof(PreferredCarbonEquivalentMin), PreferredCarbonEquivalentMin,
            nameof(PreferredCarbonEquivalentMax), PreferredCarbonEquivalentMax);

        ValidateNonNegative(nameof(ChillRiskCeiling), ChillRiskCeiling);
        ValidateNonNegative(nameof(ShrinkageRiskFloor), ShrinkageRiskFloor);

        ValidatePositive(nameof(HardnessWarningMinBhn), HardnessWarningMinBhn);
        ValidatePositive(nameof(HardnessWarningMaxBhn), HardnessWarningMaxBhn);
        ValidateOrdered(
            nameof(HardnessWarningMinBhn), HardnessWarningMinBhn,
            nameof(HardnessWarningMaxBhn), HardnessWarningMaxBhn);

        if (ShrinkageRiskFloor > ChillRiskCeiling)
        {
            throw new DomainException(
                $"Profile '{Id}': {nameof(ShrinkageRiskFloor)} ({ShrinkageRiskFloor}) " +
                $"must not exceed {nameof(ChillRiskCeiling)} ({ChillRiskCeiling}).");
        }
    }

    /// <summary>
    /// Returns true when the supplied carbon percentage is within the configured allowable range.
    /// </summary>
    public bool IsValidCarbon(double carbon) => IsWithinInclusiveRange(carbon, CarbonMin, CarbonMax);

    /// <summary>
    /// Returns true when the supplied silicon percentage is within the configured allowable range.
    /// </summary>
    public bool IsValidSilicon(double silicon) => IsWithinInclusiveRange(silicon, SiliconMin, SiliconMax);

    /// <summary>
    /// Returns true when the supplied manganese percentage is within the configured allowable range.
    /// </summary>
    public bool IsValidManganese(double manganese) => IsWithinInclusiveRange(manganese, ManganeseMin, ManganeseMax);

    /// <summary>
    /// Returns true when the supplied phosphorus percentage is within the configured allowable range.
    /// </summary>
    public bool IsValidPhosphorus(double phosphorus) => IsWithinInclusiveRange(phosphorus, PhosphorusMin, PhosphorusMax);

    /// <summary>
    /// Returns true when the supplied sulfur percentage is within the configured allowable range.
    /// </summary>
    public bool IsValidSulfur(double sulfur) => IsWithinInclusiveRange(sulfur, SulfurMin, SulfurMax);

    /// <summary>
    /// Returns true when the supplied carbon equivalent is within the preferred operating window.
    /// </summary>
    public bool IsOptimalCarbonEquivalent(double carbonEquivalent) =>
        IsWithinInclusiveRange(carbonEquivalent, PreferredCarbonEquivalentMin, PreferredCarbonEquivalentMax);

    /// <summary>
    /// Returns true when the supplied carbon equivalent is high enough to indicate reduced chill tendency.
    /// </summary>
    public bool HasLowChillRisk(double carbonEquivalent) => carbonEquivalent >= ChillRiskCeiling;

    /// <summary>
    /// Returns true when the supplied carbon equivalent is low enough to indicate elevated shrinkage sensitivity.
    /// </summary>
    public bool HasElevatedShrinkageRisk(double carbonEquivalent) => carbonEquivalent <= ShrinkageRiskFloor;

    /// <summary>
    /// Returns true when the predicted hardness is within the recommended hardness band.
    /// </summary>
    public bool IsAcceptableHardness(double hardnessBhn) =>
        IsWithinInclusiveRange(hardnessBhn, HardnessWarningMinBhn, HardnessWarningMaxBhn);

    /// <summary>
    /// Returns true when the predicted hardness is below the recommended band.
    /// </summary>
    public bool IsTooSoft(double hardnessBhn) => hardnessBhn < HardnessWarningMinBhn;

    /// <summary>
    /// Returns true when the predicted hardness is above the recommended band.
    /// </summary>
    public bool IsTooHard(double hardnessBhn) => hardnessBhn > HardnessWarningMaxBhn;

    private static bool IsWithinInclusiveRange(double value, double min, double max) =>
        value >= min && value <= max;

    private void ValidateRange(string label, double min, double max)
    {
        ValidateNonNegative($"{label}Min", min);
        ValidateNonNegative($"{label}Max", max);
        ValidateOrdered($"{label}Min", min, $"{label}Max", max);
    }

    private void RequireNonBlank(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"Profile '{Id}': {paramName} must not be blank.");
        }
    }

    private void RequirePositive(double value, string paramName)
    {
        if (value <= 0d)
        {
            throw new DomainException(
                $"Profile '{Id}': {paramName} ({value}) must be greater than zero.");
        }
    }

    private void ValidatePositive(string label, double value)
    {
        if (value <= 0d)
        {
            throw new DomainException(
                $"Profile '{Id}': {label} ({value}) must be greater than zero.");
        }
    }

    private void ValidateNonNegative(string label, double value)
    {
        if (value < 0d)
        {
            throw new DomainException(
                $"Profile '{Id}': {label} ({value}) must be greater than or equal to zero.");
        }
    }

    private void ValidateOrdered(string minLabel, double min, string maxLabel, double max)
    {
        if (min > max)
        {
            throw new DomainException(
                $"Profile '{Id}': {minLabel} ({min}) must not exceed {maxLabel} ({max}).");
        }
    }
}