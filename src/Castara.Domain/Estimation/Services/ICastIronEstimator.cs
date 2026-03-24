using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services.Strategies;

namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Coordinates cast iron property estimation by selecting and executing the appropriate
/// strategy based on the casting profile's process family.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Architectural Decision: Profile as Domain Input</strong>
/// </para>
/// <para>
/// The selected <see cref="CastingProfileDefinition"/> is part of the domain input, not just UI metadata.
/// This is intentional: process family, iron type, tuning factors, and profile-specific thresholds
/// materially affect how results should be calculated and interpreted. Different casting processes
/// (green sand, no-bake, shell mold) have fundamentally different thermal characteristics, cooling rates,
/// and metallurgical behaviors that must be reflected in the estimation algorithms.
/// </para>
/// <para>
/// <strong>Design Pattern: Strategy Selection Facade</strong>
/// </para>
/// <para>
/// This interface exists to keep the application layer simple. Callers do not need to know which
/// <see cref="ICastingEstimatorStrategy"/> handles a given profile; they only supply the inputs
/// and the selected profile. The implementation internally:
/// <list type="number">
///   <item><description>Selects the appropriate strategy based on <see cref="CastingProfileDefinition.ProcessFamily"/></description></item>
///   <item><description>Delegates estimation to that strategy</description></item>
///   <item><description>Returns results with process-specific risk flags and property predictions</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Benefits of This Approach:</strong>
/// <list type="bullet">
///   <item><description>Application layer remains simple - one call for all profile types</description></item>
///   <item><description>Domain logic is encapsulated - strategy selection is implementation detail</description></item>
///   <item><description>Extensibility - new casting processes can be added as new strategies</description></item>
///   <item><description>Testability - application layer can be tested without knowing strategy internals</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Example Usage:</strong>
/// <code>
/// // Application layer code - simple and process-agnostic
/// var inputs = new CastIronInputs(composition, section);
/// var estimate = estimator.Estimate(inputs, selectedProfile);
/// 
/// // Strategy selection happens internally based on profile.ProcessFamily
/// // Green Sand → GreenSandEstimatorStrategy
/// // No-Bake → NoBakeEstimatorStrategy
/// // Shell Mold → ShellMoldEstimatorStrategy
/// </code>
/// </para>
/// </remarks>
public interface ICastIronEstimator
{
    /// <summary>
    /// Estimates cast iron mechanical properties and identifies risk conditions using
    /// the strategy appropriate for the specified casting profile.
    /// </summary>
    /// <param name="inputs">
    /// The validated cast iron composition and section parameters to analyze.
    /// Contains chemical composition (C, Si, Mn, P, S in wt%) and section characteristics
    /// (thickness in mm, cooling rate in °C/s).
    /// </param>
    /// <param name="profile">
    /// The casting profile definition that determines which estimation strategy to use
    /// and provides process-specific tuning parameters, risk thresholds, and composition constraints.
    /// The profile's <see cref="CastingProfileDefinition.ProcessFamily"/> determines strategy selection.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing:
    /// <list type="bullet">
    ///   <item><description>Carbon equivalent and graphitization score (0-1 scale)</description></item>
    ///   <item><description>Estimated hardness range (min/max in Brinell HB)</description></item>
    ///   <item><description>Cooling and thickness adjustment factors</description></item>
    ///   <item><description>Process-specific risk flags (chill, shrinkage, hardness warnings, etc.)</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="inputs"/> or <paramref name="profile"/> is null.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when no strategy is registered for the profile's <see cref="CastingProfileDefinition.ProcessFamily"/>.
    /// This indicates a configuration error where a profile references an unsupported process family.
    /// </exception>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when estimation fails due to invalid inputs, out-of-range values, or
    /// domain rule violations detected by the selected strategy.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Strategy Selection:</strong> The implementation uses the profile's
    /// <see cref="CastingProfileDefinition.ProcessFamily"/> property to select the appropriate
    /// <see cref="ICastingEstimatorStrategy"/> implementation. Each strategy encapsulates
    /// the metallurgical models and risk assessment logic specific to that casting process.
    /// </para>
    /// <para>
    /// <strong>Process-Specific Tuning:</strong> The selected strategy uses the profile's tuning
    /// parameters (<see cref="CastingProfileDefinition.GraphitizationBias"/>, 
    /// <see cref="CastingProfileDefinition.CoolingSeverityFactor"/>, etc.) to adjust calculations
    /// for the specific thermal and metallurgical characteristics of that casting process.
    /// </para>
    /// <para>
    /// <strong>Risk Assessment:</strong> Risk flags are generated based on both strategy-specific
    /// logic and profile-specific thresholds (<see cref="CastingProfileDefinition.ChillRiskCeiling"/>, 
    /// <see cref="CastingProfileDefinition.ShrinkageRiskFloor"/>, etc.), ensuring warnings are
    /// appropriate for the selected casting process.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> Implementations should be thread-safe as the estimator
    /// may be registered as a singleton and called from multiple threads (e.g., background
    /// calculation workers).
    /// </para>
    /// </remarks>
    CastIronEstimate Estimate(
        CastIronInputs inputs,
        CastingProfileDefinition profile);
}