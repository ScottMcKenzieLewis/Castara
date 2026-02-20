using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Validation;

namespace Castara.Domain.Estimation.Services;

public sealed class CastIronEstimator : ICastIronEstimator
{
    public CastIronEstimate Estimate(CastIronInputs inputs)
    {
        var c = inputs.Composition;
        CompositionGuards.Validate(c);

        var section = inputs.Section;
        SectionGuards.Validate(section);

        var ce = ComputeCarbonEquivalent(c);
        var coolingFactor = ComputeCoolingFactor(section.CoolingRate);
        var thicknessFactor = ComputeThicknessFactor(section.ThicknessMm);

        var graphScore = ComputeGraphitizationScore(ce, coolingFactor, thicknessFactor);

        var hardness = ComputeHardness(graphScore, coolingFactor, thicknessFactor);

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
            Flags: flags);
    }

    private static double ComputeCarbonEquivalent(CastIronComposition c)
        => c.Carbon + (c.Silicon + c.Phosphorus) / 3.0;

    private static double ComputeCoolingFactor(CoolingRate rate)
        => rate switch
        {
            CoolingRate.Slow => -0.15,
            CoolingRate.Normal => 0.0,
            CoolingRate.Fast => 0.20,
            _ => 0.0
        };

    private static double ComputeThicknessFactor(double thicknessMm)
        => (CastIronEstimationConstants.ThicknessPivotMm - thicknessMm) / CastIronEstimationConstants.ThicknessScale;

    private static double ComputeGraphitizationScore(double ce, double coolingFactor, double thicknessFactor)
    {
        var raw =
            CastIronEstimationConstants.BaseGraphScore
            + CastIronEstimationConstants.CeWeight * (ce - CastIronEstimationConstants.CePivot)
            - CastIronEstimationConstants.CoolingWeight * coolingFactor
            - CastIronEstimationConstants.ThicknessWeight * thicknessFactor;

        return Clamp01(raw);
    }

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

    private static RiskSeverity ScoreToSeverity(double score01)
        => score01 >= 0.66 ? RiskSeverity.High
         : score01 >= 0.33 ? RiskSeverity.Medium
         : RiskSeverity.Low;

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static int ClampInt(int v, int min, int max) => v < min ? min : (v > max ? max : v);
}