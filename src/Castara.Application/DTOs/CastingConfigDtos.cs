using System.Collections.Generic;

namespace Castara.Application.DTOs;

/// <summary>
/// Root configuration object for deserializing casting profiles from JSON configuration.
/// </summary>
/// <remarks>
/// <para>
/// This DTO represents the top-level structure of the casting profiles configuration file
/// (typically <c>casting-profiles.json</c>). It contains a collection of profile definitions
/// that are loaded at application startup and transformed into domain models.
/// </para>
/// <para>
/// <strong>JSON Structure:</strong>
/// <code>
/// {
///   "Profiles": [
///     { /* profile 1 */ },
///     { /* profile 2 */ }
///   ]
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class CastingProfilesConfig
{
    /// <summary>
    /// Gets or initializes the collection of casting profile configurations.
    /// </summary>
    /// <value>
    /// A list of <see cref="CastingProfileConfig"/> instances. Defaults to an empty list.
    /// </value>
    public List<CastingProfileConfig> Profiles { get; init; } = [];
}

/// <summary>
/// Data transfer object representing a single casting profile configuration from JSON.
/// </summary>
/// <remarks>
/// <para>
/// This DTO maps to a complete casting profile definition in the configuration file.
/// It contains all the metadata, constraints, and tuning parameters needed for a specific
/// combination of iron type and casting process.
/// </para>
/// <para>
/// <strong>Transformation:</strong> After deserialization, instances are transformed into
/// <see cref="Castara.Domain.Casting.CastingProfileDefinition"/> domain models for use
/// throughout the application.
/// </para>
/// <para>
/// <strong>Configuration Sections:</strong>
/// <list type="bullet">
///   <item><description><see cref="Defaults"/> - Default values for typical casting parameters</description></item>
///   <item><description><see cref="Ranges"/> - Allowable composition ranges for validation</description></item>
///   <item><description><see cref="Targets"/> - Preferred targets and process-specific tuning</description></item>
///   <item><description><see cref="RiskThresholds"/> - Risk assessment thresholds</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class CastingProfileConfig
{
    /// <summary>
    /// Gets or initializes the unique identifier for this casting profile.
    /// </summary>
    /// <value>
    /// A string identifier (e.g., "GS_GRAY_30", "NB_GRAY_HIGHPROD"). Defaults to empty string.
    /// </value>
    /// <remarks>
    /// Used for profile selection, persistence, and UI display. Should be unique across all profiles.
    /// </remarks>
    public string Id { get; init; } = "";

    /// <summary>
    /// Gets or initializes the human-readable display name for this profile.
    /// </summary>
    /// <value>
    /// A display name for UI presentation (e.g., "Green Sand Gray Iron - Class 30"). Defaults to empty string.
    /// </value>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// Gets or initializes the casting process family identifier.
    /// </summary>
    /// <value>
    /// The process family (e.g., "GreenSand", "NoBake", "ShellMold"). Defaults to empty string.
    /// </value>
    /// <remarks>
    /// Used to select the appropriate estimation strategy at runtime. Must match a registered
    /// <see cref="Castara.Domain.Estimation.Services.ICastingEstimatorStrategy.SupportedProfileFamily"/>.
    /// </remarks>
    public string ProcessFamily { get; init; } = "";

    /// <summary>
    /// Gets or initializes the iron type for this profile.
    /// </summary>
    /// <value>
    /// The cast iron type (e.g., "Gray", "Ductile", "Malleable"). Defaults to empty string.
    /// </value>
    public string IronType { get; init; } = "";

    /// <summary>
    /// Gets or initializes the default values configuration section.
    /// </summary>
    /// <value>
    /// Configuration containing default parameter values. Defaults to a new instance.
    /// </value>
    public CastingDefaultsConfig Defaults { get; init; } = new();

    /// <summary>
    /// Gets or initializes the composition ranges configuration section.
    /// </summary>
    /// <value>
    /// Configuration containing allowable min/max ranges for each alloying element. Defaults to a new instance.
    /// </value>
    public CastingRangesConfig Ranges { get; init; } = new();

    /// <summary>
    /// Gets or initializes the target values configuration section.
    /// </summary>
    /// <value>
    /// Configuration containing preferred targets and process-specific tuning factors. Defaults to a new instance.
    /// </value>
    public CastingTargetsConfig Targets { get; init; } = new();

    /// <summary>
    /// Gets or initializes the risk assessment thresholds configuration section.
    /// </summary>
    /// <value>
    /// Configuration containing process-specific risk evaluation thresholds. Defaults to a new instance.
    /// </value>
    public CastingRiskThresholdsConfig RiskThresholds { get; init; } = new();
}

/// <summary>
/// Configuration section containing default parameter values for a casting profile.
/// </summary>
/// <remarks>
/// These defaults represent typical values for the casting process and are used to
/// pre-populate input fields or provide fallback values when specific data is unavailable.
/// </remarks>
public sealed class CastingDefaultsConfig
{
    /// <summary>
    /// Gets or initializes the default section thickness in millimeters.
    /// </summary>
    /// <value>
    /// The typical wall thickness for this casting profile, in millimeters. Default is 0.
    /// </value>
    /// <remarks>
    /// Represents a characteristic section thickness for this casting process.
    /// Used to pre-populate the thickness input field when this profile is selected.
    /// </remarks>
    public double SectionThicknessMm { get; init; }
}

/// <summary>
/// Configuration section containing allowable composition ranges for validation.
/// </summary>
/// <remarks>
/// <para>
/// These ranges define the acceptable boundaries for each alloying element in weight percent (wt%).
/// Values outside these ranges will fail validation and prevent calculation.
/// </para>
/// <para>
/// Ranges are process-specific because different casting methods have different constraints
/// on composition due to mold reactivity, cooling rates, and property requirements.
/// </para>
/// </remarks>
public sealed class CastingRangesConfig
{
    /// <summary>
    /// Gets or initializes the minimum allowable carbon content in weight percent.
    /// </summary>
    /// <value>The minimum carbon percentage. Default is 0.</value>
    public double CarbonMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum allowable carbon content in weight percent.
    /// </summary>
    /// <value>The maximum carbon percentage. Default is 0.</value>
    public double CarbonMax { get; init; }

    /// <summary>
    /// Gets or initializes the minimum allowable silicon content in weight percent.
    /// </summary>
    /// <value>The minimum silicon percentage. Default is 0.</value>
    public double SiliconMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum allowable silicon content in weight percent.
    /// </summary>
    /// <value>The maximum silicon percentage. Default is 0.</value>
    public double SiliconMax { get; init; }

    /// <summary>
    /// Gets or initializes the minimum allowable manganese content in weight percent.
    /// </summary>
    /// <value>The minimum manganese percentage. Default is 0.</value>
    public double ManganeseMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum allowable manganese content in weight percent.
    /// </summary>
    /// <value>The maximum manganese percentage. Default is 0.</value>
    public double ManganeseMax { get; init; }

    /// <summary>
    /// Gets or initializes the minimum allowable phosphorus content in weight percent.
    /// </summary>
    /// <value>The minimum phosphorus percentage. Default is 0.</value>
    public double PhosphorusMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum allowable phosphorus content in weight percent.
    /// </summary>
    /// <value>The maximum phosphorus percentage. Default is 0.</value>
    public double PhosphorusMax { get; init; }

    /// <summary>
    /// Gets or initializes the minimum allowable sulfur content in weight percent.
    /// </summary>
    /// <value>The minimum sulfur percentage. Default is 0.</value>
    public double SulfurMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum allowable sulfur content in weight percent.
    /// </summary>
    /// <value>The maximum sulfur percentage. Default is 0.</value>
    public double SulfurMax { get; init; }
}

/// <summary>
/// Configuration section containing target values and process-specific tuning parameters.
/// </summary>
/// <remarks>
/// <para>
/// These values guide the estimation algorithms and risk assessment logic for a specific
/// casting process. They represent:
/// <list type="bullet">
///   <item><description>Preferred compositional targets (e.g., optimal carbon equivalent range)</description></item>
///   <item><description>Process-specific bias factors that adjust calculations for the casting method</description></item>
///   <item><description>Severity multipliers that account for thermal characteristics</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class CastingTargetsConfig
{
    /// <summary>
    /// Gets or initializes the minimum preferred carbon equivalent value.
    /// </summary>
    /// <value>
    /// The lower bound of the optimal CE range. Default is 0.
    /// </value>
    /// <remarks>
    /// Values below this threshold may trigger warnings but are not invalid.
    /// Used for risk assessment and composition guidance.
    /// </remarks>
    public double PreferredCarbonEquivalentMin { get; init; }

    /// <summary>
    /// Gets or initializes the maximum preferred carbon equivalent value.
    /// </summary>
    /// <value>
    /// The upper bound of the optimal CE range. Default is 0.
    /// </value>
    /// <remarks>
    /// Values above this threshold may trigger warnings but are not invalid.
    /// Used for risk assessment and composition guidance.
    /// </remarks>
    public double PreferredCarbonEquivalentMax { get; init; }

    /// <summary>
    /// Gets or initializes the process-specific graphitization bias factor.
    /// </summary>
    /// <value>
    /// A bias value applied to graphitization calculations. Default is 0.
    /// </value>
    /// <remarks>
    /// <para>
    /// Adjusts for process variables like:
    /// <list type="bullet">
    ///   <item><description>Typical inoculation practices</description></item>
    ///   <item><description>Mold type and thermal characteristics</description></item>
    ///   <item><description>Cooling rate effects</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Positive values increase graphitization tendency, negative values decrease it.
    /// </para>
    /// </remarks>
    public double GraphitizationBias { get; init; }

    /// <summary>
    /// Gets or initializes the cooling severity multiplier for this process.
    /// </summary>
    /// <value>
    /// A multiplier for cooling rate effects on microstructure. Default is 0.
    /// </value>
    /// <remarks>
    /// <para>
    /// Accounts for process-specific cooling characteristics:
    /// <list type="bullet">
    ///   <item><description>Mold material (sand, metal, ceramic)</description></item>
    ///   <item><description>Mold thermal mass and conductivity</description></item>
    ///   <item><description>Section size effects</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Higher values indicate more severe cooling (e.g., metal molds vs. sand).
    /// </para>
    /// </remarks>
    public double CoolingSeverityFactor { get; init; }
}

/// <summary>
/// Configuration section containing risk assessment thresholds for a casting process.
/// </summary>
/// <remarks>
/// <para>
/// These thresholds are used to evaluate composition and section characteristics for
/// potential casting defects or property concerns. Thresholds are process-specific
/// because different casting methods have different susceptibilities to various risks.
/// </para>
/// </remarks>
public sealed class CastingRiskThresholdsConfig
{
    /// <summary>
    /// Gets or initializes the carbon equivalent threshold above which chill risk is low.
    /// </summary>
    /// <value>
    /// The CE ceiling for chill (white iron) risk evaluation. Default is 0.
    /// </value>
    /// <remarks>
    /// Compositions with CE above this value are considered to have low chill risk.
    /// Process-specific due to differences in mold thermal characteristics and cooling rates.
    /// </remarks>
    public double ChillRiskCeiling { get; init; }

    /// <summary>
    /// Gets or initializes the carbon equivalent threshold below which shrinkage risk increases.
    /// </summary>
    /// <value>
    /// The CE floor for shrinkage/porosity risk evaluation. Default is 0.
    /// </value>
    /// <remarks>
    /// Compositions with CE below this value are considered to have higher shrinkage risk.
    /// Process-specific based on feeding effectiveness and mold rigidity.
    /// </remarks>
    public double ShrinkageRiskFloor { get; init; }

    /// <summary>
    /// Gets or initializes the minimum hardness threshold for low-hardness warnings.
    /// </summary>
    /// <value>
    /// The minimum Brinell hardness (HBN) below which warnings are generated. Default is 0.
    /// </value>
    /// <remarks>
    /// Values below this may indicate soft spots, inadequate cooling, or composition issues.
    /// Process-specific based on typical achievable properties.
    /// </remarks>
    public double HardnessWarningMinBhn { get; init; }

    /// <summary>
    /// Gets or initializes the maximum hardness threshold for high-hardness warnings.
    /// </summary>
    /// <value>
    /// The maximum Brinell hardness (HBN) above which warnings are generated. Default is 0.
    /// </value>
    /// <remarks>
    /// Values above this may indicate chill, rapid cooling, or machinability concerns.
    /// Process-specific based on typical achievable properties and application requirements.
    /// </remarks>
    public double HardnessWarningMaxBhn { get; init; }
}