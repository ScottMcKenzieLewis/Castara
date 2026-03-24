using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Services.Strategies;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Castara.Domain.Tests.Estimation.Strategies;

public sealed class GrayIronCastingEstimatorStrategyPropertyTests
{
    private readonly GrayIronCastingEstimatorStrategy _sut = new();

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_DoesNotThrow_ForValidInputs(ValidScenario scenario)
    {
        try
        {
            _sut.Estimate(scenario.Inputs, scenario.Profile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_ProducesOrderedHardnessRange_ForValidInputs(ValidScenario scenario)
    {
        var result = _sut.Estimate(scenario.Inputs, scenario.Profile);

        return result.EstimatedHardness.MinHB <= result.EstimatedHardness.MaxHB;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_ProducesPositiveCarbonEquivalent_ForValidInputs(ValidScenario scenario)
    {
        var result = _sut.Estimate(scenario.Inputs, scenario.Profile);

        return result.CarbonEquivalent > 0.0;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_ProducesPositiveGraphitizationScore_ForValidInputs(ValidScenario scenario)
    {
        var result = _sut.Estimate(scenario.Inputs, scenario.Profile);

        return result.GraphitizationScore > 0.0;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_DifferentProfilesProduceDifferentResults_ForSameInputs(
        CastIronComposition composition,
        SectionProfile section)
    {
        var inputs = new CastIronInputs(composition, section);

        var general = GrayIronArbitraries.CreateGeneralGrayIronProfile();
        var thin = GrayIronArbitraries.CreateThinSectionGrayIronProfile();

        var generalResult = _sut.Estimate(inputs, general);
        var thinResult = _sut.Estimate(inputs, thin);

        return generalResult.GraphitizationScore != thinResult.GraphitizationScore
               || generalResult.CoolingFactor != thinResult.CoolingFactor
               || generalResult.ThicknessFactor != thinResult.ThicknessFactor
               || generalResult.EstimatedHardness.MinHB != thinResult.EstimatedHardness.MinHB
               || generalResult.EstimatedHardness.MaxHB != thinResult.EstimatedHardness.MaxHB;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_HigherCoolingSeverityFactorChangesResult_ForSameInputs(
        CastIronComposition composition,
        SectionProfile section)
    {
        var inputs = new CastIronInputs(composition, section);

        var baseline = GrayIronArbitraries.CreateGeneralGrayIronProfile();
        var moreSevere = baseline with { CoolingSeverityFactor = baseline.CoolingSeverityFactor + 0.15 };

        var baselineResult = _sut.Estimate(inputs, baseline);
        var moreSevereResult = _sut.Estimate(inputs, moreSevere);

        return baselineResult.CoolingFactor != moreSevereResult.CoolingFactor
               || baselineResult.GraphitizationScore != moreSevereResult.GraphitizationScore
               || baselineResult.EstimatedHardness.MinHB != moreSevereResult.EstimatedHardness.MinHB
               || baselineResult.EstimatedHardness.MaxHB != moreSevereResult.EstimatedHardness.MaxHB;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_HigherGraphitizationBiasChangesResult_ForSameInputs(
        CastIronComposition composition,
        SectionProfile section)
    {
        var inputs = new CastIronInputs(composition, section);

        var baseline = GrayIronArbitraries.CreateGeneralGrayIronProfile();
        var biased = baseline with { GraphitizationBias = baseline.GraphitizationBias + 0.10 };

        var baselineResult = _sut.Estimate(inputs, baseline);
        var biasedResult = _sut.Estimate(inputs, biased);

        return baselineResult.GraphitizationScore != biasedResult.GraphitizationScore
               || baselineResult.EstimatedHardness.MinHB != biasedResult.EstimatedHardness.MinHB
               || baselineResult.EstimatedHardness.MaxHB != biasedResult.EstimatedHardness.MaxHB;
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(GrayIronArbitraries)])]
    public bool Estimate_HigherCoolingRateDoesNotDecreaseMinHardness_ForSameCompositionAndProfile(
        CastIronComposition composition,
        PositiveThickness thickness)
    {
        var profile = GrayIronArbitraries.CreateGeneralGrayIronProfile();

        var lowCoolingInputs = new CastIronInputs(
            composition,
            new SectionProfile(thickness.Value, 0.50));

        var highCoolingInputs = new CastIronInputs(
            composition,
            new SectionProfile(thickness.Value, 1.75));

        var low = _sut.Estimate(lowCoolingInputs, profile);
        var high = _sut.Estimate(highCoolingInputs, profile);

        return high.EstimatedHardness.MinHB >= low.EstimatedHardness.MinHB;
    }
}

public static class GrayIronArbitraries
{
    public static Arbitrary<ValidScenario> ValidScenario()
        => Arb.From(
            from composition in ValidComposition().Generator
            from section in ValidSection().Generator
            from profile in ValidProfile().Generator
            select new ValidScenario(
                new CastIronInputs(composition, section),
                profile));

    public static Arbitrary<CastIronComposition> CastIronComposition()
        => ValidComposition();

    public static Arbitrary<SectionProfile> SectionProfile()
        => ValidSection();

    public static Arbitrary<PositiveThickness> PositiveThickness()
        => Arb.From(
            Gen.Choose(5, 50)
               .Select(x => new PositiveThickness((double)x)));

    public static Arbitrary<CastIronComposition> ValidComposition()
        => Arb.From(
            from carbon in Gen.Choose(310, 370).Select(x => x / 100.0)
            from silicon in Gen.Choose(180, 280).Select(x => x / 100.0)
            from manganese in Gen.Choose(50, 100).Select(x => x / 100.0)
            from phosphorus in Gen.Choose(0, 120).Select(x => x / 1000.0)
            from sulfur in Gen.Choose(0, 150).Select(x => x / 1000.0)
            select new CastIronComposition(
                carbon,
                silicon,
                manganese,
                phosphorus,
                sulfur));

    public static Arbitrary<SectionProfile> ValidSection()
        => Arb.From(
            from thicknessMm in Gen.Choose(5, 50).Select(x => (double)x)
            from coolingRate in Gen.Choose(10, 200).Select(x => x / 100.0)
            select new SectionProfile(
                thicknessMm,
                coolingRate));

    public static Arbitrary<CastingProfileDefinition> ValidProfile()
        => Arb.From(
            Gen.Elements(
                CreateGeneralGrayIronProfile(),
                CreateThinSectionGrayIronProfile()));

    public static CastingProfileDefinition CreateGeneralGrayIronProfile()
        => new(
            Id: "green-sand-gray-iron-general",
            DisplayName: "Green Sand - Gray Iron - General",
            ProcessFamily: "GreenSand",
            IronType: "GrayIron",
            DefaultSectionThicknessMm: 25.4,
            CarbonMin: 3.1,
            CarbonMax: 3.7,
            SiliconMin: 1.8,
            SiliconMax: 2.8,
            ManganeseMin: 0.5,
            ManganeseMax: 1.0,
            PhosphorusMin: 0.0,
            PhosphorusMax: 0.12,
            SulfurMin: 0.0,
            SulfurMax: 0.15,
            PreferredCarbonEquivalentMin: 3.9,
            PreferredCarbonEquivalentMax: 4.3,
            GraphitizationBias: 1.0,
            CoolingSeverityFactor: 1.0,
            ChillRiskCeiling: 4.4,
            ShrinkageRiskFloor: 3.7,
            HardnessWarningMinBhn: 170,
            HardnessWarningMaxBhn: 260);

    public static CastingProfileDefinition CreateThinSectionGrayIronProfile()
        => new(
            Id: "green-sand-gray-iron-thin-section",
            DisplayName: "Green Sand - Gray Iron - Thin Section",
            ProcessFamily: "GreenSand",
            IronType: "GrayIron",
            DefaultSectionThicknessMm: 12.0,
            CarbonMin: 3.2,
            CarbonMax: 3.8,
            SiliconMin: 1.9,
            SiliconMax: 2.9,
            ManganeseMin: 0.45,
            ManganeseMax: 0.9,
            PhosphorusMin: 0.0,
            PhosphorusMax: 0.10,
            SulfurMin: 0.0,
            SulfurMax: 0.12,
            PreferredCarbonEquivalentMin: 4.0,
            PreferredCarbonEquivalentMax: 4.35,
            GraphitizationBias: 1.05,
            CoolingSeverityFactor: 1.15,
            ChillRiskCeiling: 4.3,
            ShrinkageRiskFloor: 3.85,
            HardnessWarningMinBhn: 180,
            HardnessWarningMaxBhn: 285);
}

public sealed record ValidScenario(
    CastIronInputs Inputs,
    CastingProfileDefinition Profile);

public readonly record struct PositiveThickness(double Value);