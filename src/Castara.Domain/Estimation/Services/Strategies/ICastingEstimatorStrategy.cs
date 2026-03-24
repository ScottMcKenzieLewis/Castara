using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Domain.Estimation.Services.Strategies;

/// <summary>
/// Defines a strategy for estimating cast iron properties based on a specific casting process family
/// or iron type with fundamentally different metallurgical behavior.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When to Create a New Strategy (Important Design Guidance):</strong>
/// </para>
/// <para>
/// A strategy exists only when the estimation behavior changes in a <strong>meaningful</strong> way
/// that cannot be captured by profile parameters alone. Small profile differences such as default
/// thickness, threshold values, or bias factors do <strong>not</strong> justify a separate strategy
/// by themselves; those should remain profile-driven parameters that tune the same underlying algorithm.
/// </para>
/// <para>
/// <strong>Create a new strategy when:</strong>
/// <list type="bullet">
///   <item><description>The fundamental metallurgical model changes (e.g., gray iron vs. ductile iron vs. malleable iron)</description></item>
///   <item><description>Carbon equivalent calculations differ materially (different formulas, not just different biases)</description></item>
///   <item><description>Graphitization mechanisms are fundamentally different (flake vs. spheroidal vs. temper graphite)</description></item>
///   <item><description>Property prediction equations are structurally different (not just coefficient adjustments)</description></item>
///   <item><description>Risk assessment logic requires different algorithms (not just different thresholds)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Use profile parameters when:</strong>
/// <list type="bullet">
///   <item><description>The same algorithm works but needs different tuning values (cooling severity, bias factors)</description></item>
///   <item><description>Risk thresholds vary but the evaluation logic is the same (chill ceiling, shrinkage floor)</description></item>
///   <item><description>Default values differ but calculations are identical (section thickness, cooling rates)</description></item>
///   <item><description>Composition ranges vary but the model structure remains the same</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Practical Example:</strong>
/// </para>
/// <para>
/// Multiple gray iron profiles (green sand, no-bake, shell mold) may share one strategy because they
/// all follow the same metallurgical model—flake graphite formation, similar carbon equivalent formulas,
/// same hardness prediction approach. They differ in cooling rates, inoculation effectiveness, and
/// threshold values, but those are profile parameters that tune the shared algorithm.
/// </para>
/// <para>
/// In contrast, a future ductile iron model would require a different strategy because the metallurgical
/// assumptions diverge fundamentally—spheroidal graphite requires different carbon equivalent calculations,
/// nodularity affects properties differently than graphite flake morphology, and the relationship between
/// composition and hardness follows different empirical models.
/// </para>
/// <para>
/// <strong>Benefits of This Approach:</strong>
/// <list type="bullet">
///   <item><description>Keeps code maintainable—shared logic isn't duplicated across strategies</description></item>
///   <item><description>Profile configuration remains simple—process variations are just parameter changes</description></item>
///   <item><description>Strategy complexity is justified—each strategy truly represents different physics</description></item>
///   <item><description>Testing is focused—strategies test different models, profiles test different tuning</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICastingEstimatorStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle estimation for the specified casting profile.
    /// </summary>
    /// <param name="profile">
    /// The casting profile definition to evaluate for compatibility with this strategy.
    /// </param>
    /// <returns>
    /// <c>true</c> if this strategy can estimate results for the supplied profile; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Design Decision: Explicit Selection Rules</strong>
    /// </para>
    /// <para>
    /// This method exists to keep the selection rule explicit in domain code rather than burying
    /// it in dependency injection registration order or application-layer conditionals. This makes
    /// the domain logic self-documenting and ensures strategy selection is testable and predictable.
    /// </para>
    /// <para>
    /// <strong>Typical Implementation:</strong>
    /// </para>
    /// <code>
    /// public bool CanHandle(CastingProfileDefinition profile)
    /// {
    ///     // Gray iron strategy handles all gray iron profiles regardless of process
    ///     return profile.IronType.Equals("Gray", StringComparison.OrdinalIgnoreCase);
    ///     
    ///     // OR: Process-specific strategy for specialized casting methods
    ///     return profile.ProcessFamily.Equals("ShellMold", StringComparison.OrdinalIgnoreCase);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Selection Priority:</strong> When multiple strategies return <c>true</c>, the
    /// <see cref="ICastIronEstimator"/> implementation should use a deterministic selection
    /// approach (e.g., most specific match first, registration order, or explicit priority).
    /// </para>
    /// </remarks>
    bool CanHandle(CastingProfileDefinition profile);

    /// <summary>
    /// Estimates cast iron mechanical properties and identifies risk conditions using
    /// the strategy's specific metallurgical model and risk assessment logic.
    /// </summary>
    /// <param name="inputs">
    /// The validated cast iron composition and section parameters to analyze.
    /// Contains chemical composition (C, Si, Mn, P, S in wt%) and section characteristics
    /// (thickness in mm, cooling rate in °C/s).
    /// </param>
    /// <param name="profile">
    /// The casting profile definition containing process-specific tuning parameters,
    /// risk thresholds, and composition constraints that modify the strategy's base algorithms.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing:
    /// <list type="bullet">
    ///   <item><description>Carbon equivalent calculated using strategy-specific formula</description></item>
    ///   <item><description>Graphitization score (0-1 scale) adjusted by profile bias factors</description></item>
    ///   <item><description>Estimated hardness range (min/max in Brinell HB) using strategy's prediction model</description></item>
    ///   <item><description>Cooling and thickness adjustment factors</description></item>
    ///   <item><description>Risk flags based on strategy logic and profile thresholds</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="inputs"/> or <paramref name="profile"/> is null.
    /// </exception>
    /// <exception cref="Exceptions.DomainException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>Composition values are outside the strategy's supported ranges</description></item>
    ///   <item><description>Section parameters are invalid or out of physical bounds</description></item>
    ///   <item><description>Calculation results are non-physical (NaN, infinity, negative hardness, etc.)</description></item>
    ///   <item><description>Profile is not compatible with this strategy (should not happen if <see cref="CanHandle"/> is used correctly)</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Why Profile is Passed Directly (Domain Design Rationale):</strong>
    /// </para>
    /// <para>
    /// The profile is passed in directly because process assumptions belong to the domain model,
    /// not the application layer. A "green sand gray iron" estimate and a "ductile iron resin sand"
    /// estimate may require materially different reasoning even when the raw chemistry inputs look
    /// similar. The profile provides the context that determines how the strategy interprets and
    /// processes the inputs.
    /// </para>
    /// <para>
    /// <strong>Strategy Responsibilities:</strong>
    /// </para>
    /// <list type="number">
    ///   <item><description>Apply strategy-specific carbon equivalent formula (e.g., CE = C + Si/3 + P/3 for gray iron)</description></item>
    ///   <item><description>Calculate base graphitization score using strategy's metallurgical model</description></item>
    ///   <item><description>Adjust for cooling rate effects using profile's <see cref="CastingProfileDefinition.CoolingSeverityFactor"/></description></item>
    ///   <item><description>Apply profile's <see cref="CastingProfileDefinition.GraphitizationBias"/> to tune for process-specific behaviors</description></item>
    ///   <item><description>Predict hardness range using strategy's empirical equations</description></item>
    ///   <item><description>Generate risk flags by evaluating against profile thresholds with strategy-specific logic</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example Flow:</strong>
    /// </para>
    /// <code>
    /// // Strategy provides the algorithm, profile provides the tuning
    /// var ce = CalculateCarbonEquivalent(inputs.Composition); // Strategy-specific formula
    /// var baseGraphScore = CalculateGraphitization(ce, inputs.Section); // Strategy model
    /// var adjustedScore = baseGraphScore + profile.GraphitizationBias; // Profile tuning
    /// var hardness = PredictHardness(ce, adjustedScore, inputs.Section); // Strategy predictions
    /// var flags = EvaluateRisks(ce, hardness, profile); // Strategy logic + profile thresholds
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> Implementations should be stateless and thread-safe,
    /// as strategies may be registered as singletons and called concurrently.
    /// </para>
    /// </remarks>
    CastIronEstimate Estimate(
        CastIronInputs inputs,
        CastingProfileDefinition profile);
}