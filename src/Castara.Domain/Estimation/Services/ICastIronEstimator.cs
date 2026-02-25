using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Defines a service contract for estimating cast iron properties based on
/// chemical composition and section parameters.
/// </summary>
/// <remarks>
/// This estimator calculates metallurgical properties including carbon equivalent,
/// graphitization potential, and hardness predictions using empirical models
/// derived from casting industry standards and research.
/// </remarks>
public interface ICastIronEstimator
{
    /// <summary>
    /// Estimates cast iron properties including carbon equivalent, graphitization tendency,
    /// and hardness based on the provided chemical composition and section parameters.
    /// </summary>
    /// <param name="inputs">
    /// The input parameters containing chemical composition (C, Si, Mn, P, S) and
    /// section profile (thickness, cooling rate) for the analysis.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing calculated properties, derived factors,
    /// and any risk flags or warnings identified during the analysis.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="inputs"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when input parameters are outside valid ranges or contain invalid values
    /// (e.g., negative percentages, non-finite numbers).
    /// </exception>
    /// <remarks>
    /// <para>
    /// The estimation process includes:
    /// <list type="bullet">
    ///   <item><description>Carbon equivalent calculation using standard formulas</description></item>
    ///   <item><description>Graphitization potential assessment based on composition</description></item>
    ///   <item><description>Hardness prediction considering section thickness and cooling rate</description></item>
    ///   <item><description>Risk flag generation for out-of-range or concerning values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Results are estimates based on empirical models and should not be
    /// used as the sole basis for critical metallurgical decisions. Always validate with
    /// laboratory testing and consult qualified metallurgists.
    /// </para>
    /// </remarks>
    CastIronEstimate Estimate(CastIronInputs inputs);
}