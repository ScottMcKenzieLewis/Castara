using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Validation;

namespace Castara.Domain.Estimation.Services;

/// <summary>
/// Provides metallurgical property estimation for cast iron based on chemical composition
/// and section parameters using empirical models and industry-standard formulas.
/// </summary>
/// <remarks>
/// This estimator calculates carbon equivalent, graphitization potential, and hardness
/// predictions while identifying potential risks in casting operations. The calculations
/// are based on well-established metallurgical principles and casting research.
/// </remarks>
public sealed class CastIronEstimator : ICastIronEstimator
{
    /// <summary>
    /// Estimates cast iron properties including carbon equivalent, graphitization score,
    /// hardness range, and operational risk flags.
    /// </summary>
    /// <param name="inputs">
    /// The input parameters containing chemical composition and section profile data.
    /// </param>
    /// <returns>
    /// A <see cref="CastIronEstimate"/> containing all calculated properties and risk assessments.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputs"/> or its composition/section properties are null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when composition values or section parameters are outside valid ranges.
    /// </exception>
    public CastIronEstimate Estimate(CastIronInputs inputs)
    {
        // Validate inputs param
        ArgumentNullException.ThrowIfNull(inputs, nameof(inputs));
        ArgumentNullException.ThrowIfNull(inputs.Composition, nameof(inputs.Composition));
        ArgumentNullException.ThrowIfNull(inputs.Section, nameof(inputs.Section));

        // Validate chemical composition
        var c = inputs.Composition;
        CompositionGuards.Validate(c);

        // Validate section parameters
        var section = inputs.Section;
        SectionGuards.Validate(section);

        // Calculate carbon equivalent using standard formula
        var ce = ComputeCarbonEquivalent(c);

        // Convert continuous cooling rate to normalized factor
        var coolingFactor = ComputeCoolingFactor(section.CoolingRateCPerSec);

        // Calculate thickness influence on solidification
        var thicknessFactor = ComputeThicknessFactor(section.ThicknessMm);

        // Predict graphitization tendency (0-1 scale)
        var graphScore = ComputeGraphitizationScore(ce, coolingFactor, thicknessFactor);

        // Estimate hardness range in Brinell (HB)
        var hardness = ComputeHardness(graphScore, coolingFactor, thicknessFactor);

        // Generate risk flags for casting operations
        var flags = new List<RiskFlag>
        {
            BuildChillRisk(graphScore, coolingFactor, thicknessFactor),
            BuildShrinkRisk(ce, thicknessFactor, c.Manganese),
            BuildMachinabilityRisk(graphScore)
        };

        return new CastIronEstimate(
            CarbonEquivalent: Math.Round(ce, 3),
            GraphitizationScore: Math.Round(graphScore, 3),
            EstimatedHardness: hardness,
            CoolingFactor: Math.Round(coolingFactor, 3),
            ThicknessFactor: Math.Round(thicknessFactor, 3),
            Flags: flags);
    }

    /// <summary>
    /// Calculates the carbon equivalent (CE) using the standard formula:
    /// CE = %C + (%Si + %P) / 3
    /// </summary>
    /// <param name="c">The cast iron composition.</param>
    /// <returns>The carbon equivalent value.</returns>
    /// <remarks>
    /// Carbon equivalent is a key indicator of cast iron's graphitization potential
    /// and eutectic behavior. Values typically range from 3.0 to 4.5 for gray iron.
    /// </remarks>
    private static double ComputeCarbonEquivalent(CastIronComposition c)
        => c.Carbon + (c.Silicon + c.Phosphorus) / 3.0;

    /// <summary>
    /// Converts a continuous cooling rate (°C/s) into a normalized cooling factor
    /// using logarithmic interpolation to match empirical casting behavior.
    /// </summary>
    /// <param name="coolingRateCPerSec">The cooling rate in degrees Celsius per second.</param>
    /// <returns>
    /// A normalized cooling factor where:
    /// <list type="bullet">
    ///   <item><description>~-0.15 corresponds to slow cooling (~0.1 °C/s)</description></item>
    ///   <item><description>0.00 corresponds to normal cooling (~1.0 °C/s)</description></item>
    ///   <item><description>~+0.20 corresponds to fast cooling (~10 °C/s)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// The logarithmic scale reflects the non-linear relationship between cooling rate
    /// and microstructure formation. Input values are clamped between 0.02 and 50 °C/s
    /// to prevent extreme values from distorting calculations.
    /// </remarks>
    private static double ComputeCoolingFactor(double coolingRateCPerSec)
    {
        // Guard rails: avoid log(0) and tame crazy inputs
        var r = Math.Clamp(coolingRateCPerSec, 0.02, 50.0);

        // Map log10(rate) into a factor via piecewise linear interpolation
        // Anchors picked to match discrete enum behavior from previous model
        const double rSlow = 0.1;   // °C/s -> slow cooling
        const double fSlow = -0.15;

        const double rNorm = 1.0;   // °C/s -> normal cooling
        const double fNorm = 0.00;

        const double rFast = 10.0;  // °C/s -> fast cooling
        const double fFast = 0.20;

        double x = Math.Log10(r);

        double xSlow = Math.Log10(rSlow);
        double xNorm = Math.Log10(rNorm);
        double xFast = Math.Log10(rFast);

        // Piecewise linear interpolation
        if (x <= xNorm)
            return Lerp(xSlow, fSlow, xNorm, fNorm, x);

        return Lerp(xNorm, fNorm, xFast, fFast, x);
    }

    /// <summary>
    /// Performs linear interpolation between two points.
    /// </summary>
    /// <param name="x0">The x-coordinate of the first point.</param>
    /// <param name="y0">The y-coordinate of the first point.</param>
    /// <param name="x1">The x-coordinate of the second point.</param>
    /// <param name="y1">The y-coordinate of the second point.</param>
    /// <param name="x">The x-coordinate at which to interpolate.</param>
    /// <returns>The interpolated y-value at x.</returns>
    private static double Lerp(double x0, double y0, double x1, double y1, double x)
    {
        if (Math.Abs(x1 - x0) < 1e-12) return y0;
        var t = (x - x0) / (x1 - x0);
        return y0 + t * (y1 - y0);
    }

    /// <summary>
    /// Calculates the thickness factor representing the deviation from a reference
    /// section thickness and its impact on solidification characteristics.
    /// </summary>
    /// <param name="thicknessMm">The section thickness in millimeters.</param>
    /// <returns>
    /// A normalized thickness factor where positive values indicate thinner sections
    /// (faster cooling) and negative values indicate thicker sections (slower cooling).
    /// </returns>
    private static double ComputeThicknessFactor(double thicknessMm)
        => (CastIronEstimationConstants.ThicknessPivotMm - thicknessMm) / CastIronEstimationConstants.ThicknessScale;

    /// <summary>
    /// Computes the graphitization score (0-1 scale) indicating the tendency
    /// for graphite formation versus carbide formation during solidification.
    /// </summary>
    /// <param name="ce">The carbon equivalent value.</param>
    /// <param name="coolingFactor">The normalized cooling factor.</param>
    /// <param name="thicknessFactor">The normalized thickness factor.</param>
    /// <returns>
    /// A score between 0 and 1, where:
    /// <list type="bullet">
    ///   <item><description>Higher values (>0.6) indicate strong graphitization (softer, more machinable)</description></item>
    ///   <item><description>Lower values (&lt;0.4) indicate carbide tendency (harder, potential chill)</description></item>
    /// </list>
    /// </returns>
    private static double ComputeGraphitizationScore(double ce, double coolingFactor, double thicknessFactor)
    {
        var raw =
            CastIronEstimationConstants.BaseGraphScore
            + CastIronEstimationConstants.CeWeight * (ce - CastIronEstimationConstants.CePivot)
            - CastIronEstimationConstants.CoolingWeight * coolingFactor
            - CastIronEstimationConstants.ThicknessWeight * thicknessFactor;

        return Clamp01(raw);
    }

    /// <summary>
    /// Estimates the Brinell hardness (HB) range based on graphitization score
    /// and solidification conditions.
    /// </summary>
    /// <param name="graphScore">The graphitization score (0-1).</param>
    /// <param name="coolingFactor">The normalized cooling factor.</param>
    /// <param name="thicknessFactor">The normalized thickness factor.</param>
    /// <returns>
    /// A <see cref="HardnessRange"/> representing the expected hardness span in Brinell units.
    /// </returns>
    /// <remarks>
    /// Lower graphitization scores and faster cooling rates generally result in
    /// higher hardness values due to increased carbide formation and finer pearlite structures.
    /// </remarks>
    private static HardnessRange ComputeHardness(double graphScore, double coolingFactor, double thicknessFactor)
    {
        var hbCenter =
            CastIronEstimationConstants.BaseHardnessHB
            + (int)(CastIronEstimationConstants.HardnessGraphWeight * (0.55 - graphScore))
            + (int)(CastIronEstimationConstants.HardnessCoolingWeight * coolingFactor)
            + (int)(CastIronEstimationConstants.HardnessThicknessWeight * thicknessFactor);

        var min = ClampInt(
            hbCenter - CastIronEstimationConstants.HardnessSpreadHB,
            CastIronEstimationConstants.MinHardnessHB,
            CastIronEstimationConstants.MaxHardnessHB);

        var max = ClampInt(
            hbCenter + CastIronEstimationConstants.HardnessSpreadHB,
            CastIronEstimationConstants.MinHardnessHB,
            CastIronEstimationConstants.MaxHardnessHB);

        return new HardnessRange(MinHB: min, MaxHB: max);
    }

    /// <summary>
    /// Assesses the risk of chilled (carbide) structure formation in the casting.
    /// </summary>
    /// <param name="graphScore">The graphitization score (0-1).</param>
    /// <param name="coolingFactor">The normalized cooling factor.</param>
    /// <param name="thicknessFactor">The normalized thickness factor.</param>
    /// <returns>A <see cref="RiskFlag"/> indicating the severity and description of chill risk.</returns>
    /// <remarks>
    /// Chill formation occurs when cooling is too rapid for graphite to form, resulting
    /// in hard, brittle white iron structures that are difficult to machine.
    /// </remarks>
    private static RiskFlag BuildChillRisk(double graphScore, double coolingFactor, double thicknessFactor)
    {
        var score = Clamp01(0.55 - graphScore + 0.25 * coolingFactor + 0.20 * thicknessFactor);
        var sev = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "CHILL_RISK",
            Name: "Chill Risk",
            Severity: sev,
            Message: sev switch
            {
                RiskSeverity.High => "High risk of chilled structure in thin/fast-cooled sections.",
                RiskSeverity.Medium => "Moderate chill risk. Watch section thickness and cooling rate.",
                _ => "Low chill risk under current conditions."
            });
    }

    /// <summary>
    /// Evaluates the risk of shrinkage porosity and feeding issues during solidification.
    /// </summary>
    /// <param name="ce">The carbon equivalent value.</param>
    /// <param name="thicknessFactor">The normalized thickness factor.</param>
    /// <param name="mn">The manganese percentage.</param>
    /// <returns>A <see cref="RiskFlag"/> indicating the severity and description of shrinkage risk.</returns>
    /// <remarks>
    /// Shrinkage sensitivity is influenced by carbon equivalent (lower CE = more shrinkage),
    /// section thickness (thicker sections harder to feed), and manganese content.
    /// </remarks>
    private static RiskFlag BuildShrinkRisk(double ce, double thicknessFactor, double mn)
    {
        var score = Clamp01(0.25 + 0.10 * (ce - 3.6) + 0.25 * (-thicknessFactor) + 0.05 * mn);
        var sev = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "SHRINK_RISK",
            Name: "Shrink/Porosity Risk",
            Severity: sev,
            Message: sev switch
            {
                RiskSeverity.High => "High feeding sensitivity. Review risering/feeding plan.",
                RiskSeverity.Medium => "Moderate feeding sensitivity. Confirm gating/risers.",
                _ => "Low feeding sensitivity expected."
            });
    }

    /// <summary>
    /// Assesses machinability concerns based on expected microstructure.
    /// </summary>
    /// <param name="graphScore">The graphitization score (0-1).</param>
    /// <returns>A <see cref="RiskFlag"/> indicating potential machinability challenges.</returns>
    /// <remarks>
    /// Lower graphitization scores indicate harder structures with more carbides,
    /// which can increase tool wear and reduce machinability.
    /// </remarks>
    private static RiskFlag BuildMachinabilityRisk(double graphScore)
    {
        var score = Clamp01(0.55 - graphScore);
        var sev = ScoreToSeverity(score);

        return new RiskFlag(
            Code: "MACHINABILITY",
            Name: "Machinability Concern",
            Severity: sev,
            Message: sev switch
            {
                RiskSeverity.High => "Machinability may be challenging (harder structure).",
                RiskSeverity.Medium => "Machinability likely acceptable. Verify hardness.",
                _ => "Machinability likely good (more graphitic)."
            });
    }

    /// <summary>
    /// Converts a normalized risk score (0-1) to a severity level.
    /// </summary>
    /// <param name="score01">A risk score between 0 and 1.</param>
    /// <returns>
    /// A <see cref="RiskSeverity"/> where:
    /// <list type="bullet">
    ///   <item><description>High: score >= 0.66</description></item>
    ///   <item><description>Medium: 0.33 &lt;= score &lt; 0.66</description></item>
    ///   <item><description>Low: score &lt; 0.33</description></item>
    /// </list>
    /// </returns>
    private static RiskSeverity ScoreToSeverity(double score01)
        => score01 >= 0.66 ? RiskSeverity.High
         : score01 >= 0.33 ? RiskSeverity.Medium
         : RiskSeverity.Low;

    /// <summary>
    /// Clamps a double value to the range [0, 1].
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    /// <summary>
    /// Clamps an integer value to a specified range.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <param name="min">The minimum allowable value.</param>
    /// <param name="max">The maximum allowable value.</param>
    /// <returns>The clamped value.</returns>
    private static int ClampInt(int v, int min, int max) => v < min ? min : (v > max ? max : v);
}