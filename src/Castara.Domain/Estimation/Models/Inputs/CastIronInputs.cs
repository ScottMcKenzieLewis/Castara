using Castara.Domain.Composition;

namespace Castara.Domain.Estimation.Models.Inputs;

/// <summary>
/// Encapsulates all input parameters required for cast iron property estimation,
/// combining chemical composition data with section profile characteristics.
/// </summary>
/// <param name="Composition">
/// The chemical composition of the cast iron, including weight percentages of
/// carbon, silicon, manganese, phosphorus, and sulfur.
/// </param>
/// <param name="Section">
/// The physical section parameters including thickness and cooling rate that
/// influence solidification behavior and microstructure development.
/// </param>
/// <remarks>
/// <para>
/// This record serves as the complete input data contract for the
/// <see cref="Services.ICastIronEstimator"/> service. Both composition and section
/// parameters are required and will be validated before estimation calculations begin.
/// </para>
/// <para>
/// <strong>Validation:</strong> Input validation is performed automatically during
/// the estimation process using:
/// <list type="bullet">
///   <item><description><see cref="Castara.Domain.Composition.CompositionGuards"/> for chemical composition</description></item>
///   <item><description><see cref="Validation.SectionGuards"/> for section parameters</description></item>
/// </list>
/// </para>
/// <para>
/// The combination of composition and section parameters determines:
/// <list type="bullet">
///   <item><description>Carbon equivalent and graphitization potential</description></item>
///   <item><description>Expected microstructure (graphite vs. carbide formation)</description></item>
///   <item><description>Predicted hardness range</description></item>
///   <item><description>Risk factors for casting defects and quality concerns</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example of typical gray iron inputs:
/// <code>
/// var composition = new CastIronComposition(
///     Carbon: 3.4,
///     Silicon: 2.1,
///     Manganese: 0.7,
///     Phosphorus: 0.05,
///     Sulfur: 0.08
/// );
/// 
/// var section = new SectionProfile(
///     ThicknessMm: 25.0,
///     CoolingRateCPerSec: 1.5
/// );
/// 
/// var inputs = new CastIronInputs(
///     Composition: composition,
///     Section: section
/// );
/// 
/// // Use with estimator
/// var estimator = new CastIronEstimator();
/// var estimate = estimator.Estimate(inputs);
/// </code>
/// </example>
public sealed record CastIronInputs(
    CastIronComposition Composition,
    SectionProfile Section);