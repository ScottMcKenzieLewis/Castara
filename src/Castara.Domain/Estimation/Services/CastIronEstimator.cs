using System;
using System.Collections.Generic;
using System.Linq;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services.Strategies;

namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Default implementation of <see cref="ICastIronEstimator"/> that acts as a facade,
/// delegating to the first registered strategy capable of handling the selected casting profile.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Architectural Role: Facade + Strategy Selection</strong>
/// </para>
/// <para>
/// This class centralizes strategy selection so callers never need to branch on process family
/// or iron type themselves. That keeps "which model applies?" as a domain concern instead of a
/// UI concern. The application layer simply calls <c>Estimate(inputs, profile)</c> and the
/// appropriate metallurgical model is automatically selected and executed.
/// </para>
/// <para>
/// <strong>Strategy Resolution:</strong>
/// </para>
/// <para>
/// Strategies are evaluated in registration order, and the first strategy where
/// <see cref="ICastingEstimatorStrategy.CanHandle"/> returns <c>true</c> is selected.
/// This allows for flexibility in strategy organization:
/// <list type="bullet">
///   <item><description>Specific strategies can be registered first (e.g., shell mold gray iron)</description></item>
///   <item><description>General strategies can be registered last as fallbacks (e.g., all gray iron)</description></item>
///   <item><description>Multiple iron types can coexist (gray iron strategy, ductile iron strategy, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Error Handling Philosophy:</strong>
/// </para>
/// <para>
/// Failure to resolve a strategy is treated as a <strong>configuration/modeling error</strong> rather
/// than a recoverable runtime condition. This is intentional: a selected profile without a compatible
/// estimator means the application has been wired inconsistently. This indicates one of:
/// <list type="bullet">
///   <item><description>A profile was loaded that references an unsupported process family or iron type</description></item>
///   <item><description>The required strategy implementation was not registered in dependency injection</description></item>
///   <item><description>Strategy <c>CanHandle</c> logic doesn't match the profile definitions</description></item>
/// </list>
/// </para>
/// <para>
/// Treating this as an exception forces immediate attention during development/deployment rather than
/// allowing the application to run in an invalid state.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This implementation is thread-safe. The strategy collection is
/// immutable after construction, and strategies themselves should be stateless and thread-safe.
/// </para>
/// <para>
/// <strong>Example DI Registration:</strong>
/// <code>
/// // Register strategies in order (specific to general)
/// services.AddSingleton&lt;ICastingEstimatorStrategy, ShellMoldGrayIronStrategy&gt;();
/// services.AddSingleton&lt;ICastingEstimatorStrategy, GrayIronStrategy&gt;(); // Fallback
/// services.AddSingleton&lt;ICastingEstimatorStrategy, DuctileIronStrategy&gt;();
/// 
/// // Register facade (receives all strategies via IEnumerable)
/// services.AddSingleton&lt;ICastIronEstimator, CastIronEstimator&gt;();
/// </code>
/// </para>
/// </remarks>
public sealed class CastIronEstimator : ICastIronEstimator
{
    // ============================================================
    // Fields
    // ============================================================

    /// <summary>
    /// Immutable collection of registered estimation strategies, evaluated in order.
    /// </summary>
    private readonly IReadOnlyList<ICastingEstimatorStrategy> _strategies;

    // ============================================================
    // Constructor
    // ============================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="CastIronEstimator"/> class with
    /// the specified collection of estimation strategies.
    /// </summary>
    /// <param name="strategies">
    /// The collection of <see cref="ICastingEstimatorStrategy"/> implementations to use
    /// for estimation. Strategies are evaluated in enumeration order, and the first
    /// strategy that returns <c>true</c> from <see cref="ICastingEstimatorStrategy.CanHandle"/>
    /// is selected.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="strategies"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The strategies collection is typically populated by dependency injection, which
    /// registers all <see cref="ICastingEstimatorStrategy"/> implementations and injects
    /// them as an <see cref="IEnumerable{T}"/>.
    /// </para>
    /// <para>
    /// <strong>Empty Collection Handling:</strong> An empty strategies collection is allowed
    /// but will cause all <see cref="Estimate"/> calls to throw <see cref="InvalidOperationException"/>
    /// . This is acceptable because it indicates a configuration error that should be caught
    /// during application startup or integration testing.
    /// </para>
    /// </remarks>
    public CastIronEstimator(IEnumerable<ICastingEstimatorStrategy> strategies)
    {
        _strategies = strategies?.ToList()
            ?? throw new ArgumentNullException(nameof(strategies));
    }

    // ============================================================
    // Public Methods
    // ============================================================

    /// <summary>
    /// Estimates cast iron mechanical properties by selecting and executing the appropriate
    /// estimation strategy for the specified casting profile.
    /// </summary>
    /// <param name="inputs">
    /// The validated cast iron composition and section parameters to analyze.
    /// Contains chemical composition (C, Si, Mn, P, S in wt%) and section characteristics
    /// (thickness in mm, cooling rate in °C/s).
    /// </param>
    /// <param name="profile">
    /// The casting profile definition that determines which estimation strategy to use
    /// and provides process-specific tuning parameters, risk thresholds, and composition constraints.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing carbon equivalent, graphitization score,
    /// hardness range, adjustment factors, and process-specific risk flags.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputs"/> or <paramref name="profile"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no registered strategy returns <c>true</c> from
    /// <see cref="ICastingEstimatorStrategy.CanHandle"/> for the specified profile.
    /// This indicates a configuration error where a profile exists without a compatible strategy.
    /// </exception>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when the selected strategy encounters domain rule violations during estimation
    /// (invalid inputs, out-of-range values, non-physical results, etc.).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Strategy Selection Process:</strong>
    /// <list type="number">
    ///   <item><description>Iterate through registered strategies in order</description></item>
    ///   <item><description>Call <see cref="ICastingEstimatorStrategy.CanHandle"/> on each strategy</description></item>
    ///   <item><description>Select the first strategy that returns <c>true</c></description></item>
    ///   <item><description>Delegate estimation to the selected strategy</description></item>
    ///   <item><description>Return the strategy's result to the caller</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>No Strategy Match:</strong> If no strategy can handle the profile, an
    /// <see cref="InvalidOperationException"/> is thrown with a detailed message including
    /// the profile ID, iron type, and process family. This helps diagnose configuration issues
    /// during development and deployment.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Strategy selection uses <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// with early exit, so only strategies before the matching one are evaluated. Register
    /// more commonly used strategies first for optimal performance.
    /// </para>
    /// </remarks>
    public CastIronEstimate Estimate(
        CastIronInputs inputs,
        CastingProfileDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(profile);

        // Find the first strategy capable of handling this profile
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(profile));

        if (strategy is null)
        {
            throw new InvalidOperationException(
                $"No casting estimator strategy is registered for profile '{profile.Id}' " +
                $"({profile.IronType} / {profile.ProcessFamily}). " +
                $"Ensure a compatible strategy is registered in dependency injection.");
        }

        // Delegate to the selected strategy
        return strategy.Estimate(inputs, profile);
    }
}