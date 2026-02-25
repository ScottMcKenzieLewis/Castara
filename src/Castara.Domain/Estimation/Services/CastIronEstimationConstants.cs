namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Provides calibrated constants for cast iron property estimation models.
/// </summary>
/// <remarks>
/// These constants have been tuned based on empirical data from cast iron research
/// and industry standards. Modifications to these values will affect all estimation
/// calculations and should be validated against known casting results.
/// </remarks>
internal static class CastIronEstimationConstants
{
    // ==========================================
    // Section Thickness Parameters
    // ==========================================

    /// <summary>
    /// The reference section thickness in millimeters used as the baseline for
    /// thickness factor calculations.
    /// </summary>
    /// <remarks>
    /// Value: 20.0 mm represents a typical moderate section thickness in gray iron castings.
    /// Thinner sections cool faster and are more prone to chill formation.
    /// Thicker sections cool slower and may exhibit coarser graphite structures.
    /// </remarks>
    public const double ThicknessPivotMm = 20.0;

    /// <summary>
    /// The scaling factor used to normalize thickness deviations from the pivot.
    /// </summary>
    /// <remarks>
    /// Value: 80.0 mm provides appropriate sensitivity across the typical casting
    /// thickness range (5-100 mm). A larger scale reduces the impact of thickness
    /// variations on final calculations.
    /// </remarks>
    public const double ThicknessScale = 80.0;

    // ==========================================
    // Graphitization Score Parameters
    // ==========================================

    /// <summary>
    /// The baseline graphitization score before applying composition and section adjustments.
    /// </summary>
    /// <remarks>
    /// Value: 0.45 (on a 0-1 scale) represents neutral graphitization tendency.
    /// This baseline is modified by carbon equivalent, cooling rate, and thickness
    /// to produce the final graphitization score.
    /// </remarks>
    public const double BaseGraphScore = 0.45;

    /// <summary>
    /// The reference carbon equivalent value used as the baseline for CE adjustments.
    /// </summary>
    /// <remarks>
    /// Value: 3.6 represents a typical carbon equivalent for gray iron.
    /// CE values above this pivot promote graphitization; values below increase
    /// carbide formation tendency.
    /// </remarks>
    public const double CePivot = 3.6;

    /// <summary>
    /// The weight factor for carbon equivalent deviation in graphitization calculations.
    /// </summary>
    /// <remarks>
    /// Value: 0.12 controls how strongly CE changes affect the graphitization score.
    /// Each 0.1 change in CE from the pivot shifts the graphitization score by ±0.012.
    /// </remarks>
    public const double CeWeight = 0.12;

    /// <summary>
    /// The weight factor for cooling rate influence on graphitization.
    /// </summary>
    /// <remarks>
    /// Value: 0.25 reflects the strong impact of cooling rate on microstructure.
    /// Faster cooling (positive cooling factor) reduces graphitization by inhibiting
    /// graphite nucleation and growth.
    /// </remarks>
    public const double CoolingWeight = 0.25;

    /// <summary>
    /// The weight factor for section thickness influence on graphitization.
    /// </summary>
    /// <remarks>
    /// Value: 0.20 accounts for the relationship between section size and cooling behavior.
    /// Thinner sections (positive thickness factor) cool faster and reduce graphitization.
    /// </remarks>
    public const double ThicknessWeight = 0.20;

    // ==========================================
    // Hardness Estimation Parameters
    // ==========================================

    /// <summary>
    /// The baseline Brinell hardness (HB) before applying microstructure and section adjustments.
    /// </summary>
    /// <remarks>
    /// Value: 220 HB represents typical gray iron hardness with moderate graphitization.
    /// This baseline is adjusted based on graphitization score, cooling rate, and thickness.
    /// </remarks>
    public const int BaseHardnessHB = 220;

    /// <summary>
    /// The weight factor for graphitization score influence on hardness (Brinell units).
    /// </summary>
    /// <remarks>
    /// Value: 60.0 HB reflects the inverse relationship between graphitization and hardness.
    /// Lower graphitization (more carbides) increases hardness; higher graphitization
    /// (more graphite) decreases hardness.
    /// </remarks>
    public const double HardnessGraphWeight = 60.0;

    /// <summary>
    /// The weight factor for cooling rate influence on hardness (Brinell units).
    /// </summary>
    /// <remarks>
    /// Value: 35.0 HB accounts for the hardening effect of faster cooling rates,
    /// which produce finer pearlite structures and increased carbide content.
    /// </remarks>
    public const double HardnessCoolingWeight = 35.0;

    /// <summary>
    /// The weight factor for section thickness influence on hardness (Brinell units).
    /// </summary>
    /// <remarks>
    /// Value: 25.0 HB reflects the relationship between section size and as-cast hardness.
    /// Thinner sections cool faster and typically exhibit higher hardness.
    /// </remarks>
    public const double HardnessThicknessWeight = 25.0;

    /// <summary>
    /// The hardness range spread (±) around the calculated center value (Brinell units).
    /// </summary>
    /// <remarks>
    /// Value: ±15 HB accounts for natural variation in cast iron hardness due to
    /// localized composition variations, cooling rate differences, and measurement uncertainty.
    /// </remarks>
    public const int HardnessSpreadHB = 15;

    /// <summary>
    /// The minimum realistic Brinell hardness for cast iron structures.
    /// </summary>
    /// <remarks>
    /// Value: 140 HB corresponds to very soft, highly graphitic gray iron.
    /// Values below this are unrealistic for typical cast iron compositions.
    /// </remarks>
    public const int MinHardnessHB = 140;

    /// <summary>
    /// The maximum realistic Brinell hardness for cast iron structures.
    /// </summary>
    /// <remarks>
    /// Value: 320 HB corresponds to very hard white iron with extensive carbides.
    /// Values above this would require special compositions or heat treatments.
    /// </remarks>
    public const int MaxHardnessHB = 320;
}