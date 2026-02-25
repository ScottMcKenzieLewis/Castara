namespace Castara.Domain.Estimation.Models.Inputs;

/// <summary>
/// Represents the physical section parameters that influence solidification behavior
/// and microstructure development in cast iron.
/// </summary>
/// <param name="ThicknessMm">
/// The section thickness in millimeters. Must be a positive, finite value.
/// Typical range: 5-100 mm for most gray iron castings.
/// </param>
/// <param name="CoolingRateCPerSec">
/// The estimated cooling rate in degrees Celsius per second during solidification.
/// Must be a positive, finite value not exceeding 200 °C/s (realistic upper limit).
/// Typical range: 0.5-10 °C/s for sand castings; 5-50 °C/s for metal mold castings.
/// </param>
/// <remarks>
/// <para>
/// Section thickness and cooling rate are critical parameters that determine the
/// microstructure and properties of cast iron. These parameters affect:
/// <list type="bullet">
///   <item><description>Graphite morphology and distribution</description></item>
///   <item><description>Tendency for carbide (chill) formation</description></item>
///   <item><description>Matrix structure (ferrite vs. pearlite)</description></item>
///   <item><description>Final hardness and machinability</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Thickness Guidelines:</strong>
/// </para>
/// <para>
/// Thin sections (&lt;10 mm) cool rapidly and are more prone to chill formation,
/// requiring higher carbon equivalent or inoculant additions. Thick sections (&gt;50 mm)
/// cool slowly and may exhibit coarser graphite structures with lower hardness.
/// </para>
/// <para>
/// <strong>Cooling Rate Guidelines:</strong>
/// </para>
/// <para>
/// Slow cooling (&lt;1 °C/s) promotes graphitization and softer structures but may
/// increase grain size. Fast cooling (&gt;5 °C/s) increases chill risk and hardness
/// but can produce finer microstructures. The cooling rate is influenced by mold
/// material (sand, metal), pouring temperature, and section geometry.
/// </para>
/// <para>
/// <strong>Validation:</strong> Input values are validated by <see cref="Validation.SectionGuards"/>
/// to ensure they are finite, positive, and within realistic ranges for casting operations.
/// </para>
/// </remarks>
/// <example>
/// Example section profiles for different casting scenarios:
/// <code>
/// // Typical sand casting - moderate thickness and cooling rate
/// var sandCasting = new SectionProfile(
///     ThicknessMm: 25.0,
///     CoolingRateCPerSec: 1.5
/// );
/// 
/// // Thin section - higher chill risk
/// var thinSection = new SectionProfile(
///     ThicknessMm: 8.0,
///     CoolingRateCPerSec: 5.0
/// );
/// 
/// // Heavy section - slower cooling
/// var heavySection = new SectionProfile(
///     ThicknessMm: 75.0,
///     CoolingRateCPerSec: 0.8
/// );
/// </code>
/// </example>
public sealed record SectionProfile(
    double ThicknessMm,
    double CoolingRateCPerSec);