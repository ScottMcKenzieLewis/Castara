using Castara.Domain.Estimation.Validation;

namespace Castara.Domain.Composition;

/// <summary>
/// Represents the chemical composition of cast iron, defining the weight percentages
/// of key alloying elements that determine metallurgical properties and casting behavior.
/// </summary>
/// <param name="Carbon">
/// Carbon content in weight percent (wt%). Typical range: 2.5-4.0% for gray iron.
/// Primary graphite-forming element; higher carbon promotes graphitization and reduces shrinkage.
/// </param>
/// <param name="Silicon">
/// Silicon content in weight percent (wt%). Typical range: 1.0-3.0% for gray iron.
/// Strong graphite promoter; increases fluidity and carbon equivalent. Excessive silicon (&gt;3%)
/// can cause flotation defects.
/// </param>
/// <param name="Manganese">
/// Manganese content in weight percent (wt%). Typical range: 0.4-1.2% for gray iron.
/// Pearlite stabilizer; increases hardness and strength. Combines with sulfur to form MnS inclusions,
/// preventing detrimental iron sulfide formation.
/// </param>
/// <param name="Phosphorus">
/// Phosphorus content in weight percent (wt%). Typical range: 0.02-0.15% for gray iron.
/// Contributes to carbon equivalent and improves fluidity. High phosphorus (&gt;0.3%) can cause
/// brittleness due to iron phosphide (steadite) formation.
/// </param>
/// <param name="Sulfur">
/// Sulfur content in weight percent (wt%). Typical range: 0.05-0.15% for gray iron.
/// Carbide stabilizer; opposes graphitization. Excessive sulfur can cause gas porosity and
/// reduce fluidity. Typically controlled by manganese addition (Mn:S ratio ~3:1 or higher).
/// </param>
/// <remarks>
/// <para>
/// The chemical composition is the primary factor controlling cast iron microstructure,
/// mechanical properties, and casting characteristics. These five elements interact to
/// influence:
/// <list type="bullet">
///   <item><description>Carbon equivalent (CE = %C + (%Si + %P) / 3)</description></item>
///   <item><description>Graphitization tendency vs. carbide formation</description></item>
///   <item><description>Matrix structure (ferrite, pearlite, or mixed)</description></item>
///   <item><description>Fluidity and castability</description></item>
///   <item><description>Shrinkage behavior and feeding requirements</description></item>
///   <item><description>Final mechanical properties (hardness, strength, machinability)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Element Interactions:</strong>
/// </para>
/// <para>
/// <strong>Carbon + Silicon:</strong> Work synergistically to promote graphitization.
/// The carbon equivalent formula (CE) combines these effects.
/// </para>
/// <para>
/// <strong>Manganese + Sulfur:</strong> Manganese neutralizes sulfur's negative effects
/// by forming stable MnS inclusions. Insufficient manganese (low Mn:S ratio) can lead to
/// poor casting quality.
/// </para>
/// <para>
/// <strong>Silicon vs. Sulfur:</strong> Silicon promotes graphitization while sulfur
/// promotes carbides, creating opposing tendencies that must be balanced.
/// </para>
/// <para>
/// <strong>Validation:</strong> Composition values are validated by <see cref="CompositionGuards"/>
/// to ensure all percentages are finite, non-negative, and within realistic ranges for
/// cast iron production.
/// </para>
/// </remarks>
/// <example>
/// Example compositions for different gray iron grades:
/// <code>
/// // Class 20 Gray Iron - lower strength, good machinability
/// var class20 = new CastIronComposition(
///     Carbon: 3.5,
///     Silicon: 2.4,
///     Manganese: 0.6,
///     Phosphorus: 0.08,
///     Sulfur: 0.10
/// );
/// 
/// // Class 30 Gray Iron - balanced properties
/// var class30 = new CastIronComposition(
///     Carbon: 3.2,
///     Silicon: 2.0,
///     Manganese: 0.8,
///     Phosphorus: 0.05,
///     Sulfur: 0.08
/// );
/// 
/// // Class 40 Gray Iron - higher strength, moderate machinability
/// var class40 = new CastIronComposition(
///     Carbon: 3.0,
///     Silicon: 1.8,
///     Manganese: 0.9,
///     Phosphorus: 0.04,
///     Sulfur: 0.07
/// );
/// </code>
/// </example>
public sealed record CastIronComposition(
    double Carbon,
    double Silicon,
    double Manganese,
    double Phosphorus,
    double Sulfur);