using System;
using System.Linq;
using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Xunit;

namespace Castara.Domain.Tests.Estimation.Engine;

public sealed class CastIronEstimatorTests
{
    private static CastIronEstimator CreateSut() => new();

    private static CastIronInputs BaselineInputs()
        => new(
            new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03),
            new SectionProfile(20, CoolingRate.Normal));

    [Fact]
    public void Estimate_Computes_CarbonEquivalent_RoundedTo3()
    {
        var sut = CreateSut();

        var result = sut.Estimate(BaselineInputs());

        // CE = 3.6 + (2.2 + 0.05)/3 = 4.35
        Assert.Equal(4.350, result.CarbonEquivalent, 3);
    }

    [Fact]
    public void Estimate_Returns_Valid_HardnessRange()
    {
        var sut = CreateSut();

        var result = sut.Estimate(BaselineInputs());

        Assert.True(result.EstimatedHardness.MinHB <= result.EstimatedHardness.MaxHB);
        Assert.InRange(result.EstimatedHardness.MinHB, 140, 320);
        Assert.InRange(result.EstimatedHardness.MaxHB, 140, 320);
    }

    [Fact]
    public void Estimate_Emits_Expected_RiskFlagCodes_Unique()
    {
        var sut = CreateSut();

        var result = sut.Estimate(BaselineInputs());

        var codes = result.Flags.Select(f => f.Code).ToArray();

        Assert.Equal(3, codes.Length);
        Assert.Contains("CHILL_RISK", codes);
        Assert.Contains("SHRINK_RISK", codes);
        Assert.Contains("MACHINABILITY", codes);

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    [Fact]
    public void Baseline_Tends_To_Low_ChillRisk()
    {
        var sut = CreateSut();

        var result = sut.Estimate(BaselineInputs());

        var chill = Assert.Single(result.Flags, f => f.Code == "CHILL_RISK");
        Assert.Equal(RiskSeverity.Low, chill.Severity);
    }

    [Fact]
    public void ThinFast_Should_NotLower_ChillRisk_Versus_Baseline()
    {
        var sut = CreateSut();

        var baseline = sut.Estimate(BaselineInputs());

        var thinFast = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.4, 1.9, 0.4, 0.04, 0.04),
            new SectionProfile(6, CoolingRate.Fast)));

        var baseChill = Assert.Single(baseline.Flags, f => f.Code == "CHILL_RISK");
        var tfChill = Assert.Single(thinFast.Flags, f => f.Code == "CHILL_RISK");

        Assert.True((int)tfChill.Severity >= (int)baseChill.Severity);
    }

    [Fact]
    public void ThickSlow_Should_Increase_GraphitizationScore_Versus_ThinFast()
    {
        var sut = CreateSut();

        var thickSlow = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.8, 2.4, 0.25, 0.06, 0.02),
            new SectionProfile(60, CoolingRate.Slow)));

        var thinFast = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.4, 1.9, 0.4, 0.04, 0.04),
            new SectionProfile(6, CoolingRate.Fast)));

        Assert.InRange(thickSlow.GraphitizationScore, 0.0, 1.0);
        Assert.InRange(thinFast.GraphitizationScore, 0.0, 1.0);

        Assert.True(thickSlow.GraphitizationScore > thinFast.GraphitizationScore);
    }

    [Fact]
    public void ShrinkRisk_Can_Be_High_For_ThickSection_HighCE_HighMn()
    {
        var sut = CreateSut();

        // Push shrink score high:
        // - Very thick section (drives -thicknessFactor positive)
        // - High CE (high C/Si/P)
        // - Higher Mn
        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(
                Carbon: 4.5,
                Silicon: 3.5,
                Manganese: 1.5,
                Phosphorus: 0.30,
                Sulfur: 0.02),
            new SectionProfile(200, CoolingRate.Slow)));

        var shrink = Assert.Single(result.Flags, f => f.Code == "SHRINK_RISK");
        Assert.Equal(RiskSeverity.High, shrink.Severity);
    }

    [Fact]
    public void GraphitizationScore_Clamps_To_One_For_Extreme_ThickSlow_HighCE()
    {
        var sut = CreateSut();

        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(
                Carbon: 4.5,
                Silicon: 3.5,
                Manganese: 0.3,
                Phosphorus: 0.30,
                Sulfur: 0.02),
            new SectionProfile(200, CoolingRate.Slow)));

        // You round to 3 decimals in the domain object; clamped upper bound should become 1.000
        Assert.Equal(1.000, result.GraphitizationScore, 3);
    }

    [Fact]
    public void Estimate_Throws_When_Composition_OutOfRange()
    {
        var sut = CreateSut();

        var bad = new CastIronInputs(
            new CastIronComposition(10.0, 2.2, 0.3, 0.05, 0.03), // Carbon invalid
            new SectionProfile(20, CoolingRate.Normal));

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Estimate(bad));
    }

    [Fact]
    public void Estimate_Throws_When_Composition_Component_OutOfRange()
    {
        var sut = CreateSut();

        // Silicon below allowed range (per CompositionGuards)
        var bad = new CastIronInputs(
            new CastIronComposition(3.6, 0.1, 0.3, 0.05, 0.03),
            new SectionProfile(20, CoolingRate.Normal));

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Estimate(bad));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(250.0)]
    public void Estimate_Throws_When_Thickness_OutOfRange(double thicknessMm)
    {
        var sut = CreateSut();

        var inputs = new CastIronInputs(
            new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03),
            new SectionProfile(thicknessMm, CoolingRate.Normal));

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Estimate(inputs));
    }

    [Fact]
    public void ChillRisk_Should_Be_Medium_At_BoundaryOrAbove_033()
    {
        var sut = CreateSut();

        // Drive CHILL_RISK score upward enough to cross ~0.33
        // score = Clamp01(0.55 - graphScore + 0.25*cooling + 0.20*thicknessFactor)
        // Choose values that keep graphScore relatively low and add thicknessFactor + coolingFactor
        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.0, 0.6, 0.3, 0.0, 0.02), // low CE -> lowers graphScore
            new SectionProfile(2.0, CoolingRate.Fast)));       // thin + fast -> raises chill score

        var chill = Assert.Single(result.Flags, f => f.Code == "CHILL_RISK");

        // We expect Medium or High. High may not be reachable depending on your constants, Medium should be.
        Assert.True(chill.Severity is RiskSeverity.Medium or RiskSeverity.High);
    }

    [Fact]
    public void ShrinkRisk_Should_Be_Low_On_Thin_Normal_LowCE()
    {
        var sut = CreateSut();

        // Drive SHRINK_RISK score down
        // score = 0.25 + 0.10*(ce-3.6) + 0.25*(-thicknessFactor) + 0.05*mn
        // Thin section => thicknessFactor positive => (-thicknessFactor) negative => lowers score
        // Low CE and low Mn lower score further
        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(2.5, 0.5, 0.0, 0.0, 0.02),
            new SectionProfile(2.0, CoolingRate.Normal)));

        var shrink = Assert.Single(result.Flags, f => f.Code == "SHRINK_RISK");
        Assert.Equal(RiskSeverity.Low, shrink.Severity);
    }

    [Fact]
    public void GraphitizationScore_Should_Be_Low_For_Extreme_ThinFast_LowCE()
    {
        var sut = CreateSut();

        // Minimum-ish values within CompositionGuards
        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(
                Carbon: 2.5,
                Silicon: 0.5,
                Manganese: 0.3,
                Phosphorus: 0.0,
                Sulfur: 0.02),
            new SectionProfile(2.0, CoolingRate.Fast)));

        Assert.InRange(result.GraphitizationScore, 0.0, 1.0);

        // Based on your current constants, this scenario bottoms out around ~0.243.
        // We assert "low" rather than a clamp-to-zero (which is unreachable).
        Assert.True(result.GraphitizationScore <= 0.30, $"Expected a low graph score, got {result.GraphitizationScore:0.000}");
    }

    [Fact]
    public void Hardness_Should_Clamp_Within_Bounds_For_Extreme_ThinFast_LowGraph()
    {
        var sut = CreateSut();

        // This scenario tends toward very low graphScore (harder),
        // potentially pushing hbCenter upward; we assert clamping remains within 140..320.
        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(2.5, 0.5, 0.3, 0.0, 0.02),
            new SectionProfile(2.0, CoolingRate.Fast)));

        Assert.InRange(result.EstimatedHardness.MinHB, 140, 320);
        Assert.InRange(result.EstimatedHardness.MaxHB, 140, 320);
        Assert.True(result.EstimatedHardness.MinHB <= result.EstimatedHardness.MaxHB);
    }

    [Fact]
    public void CoolingRate_Slow_Should_Not_Throw_And_Should_Produce_Stable_Result()
    {
        var sut = CreateSut();

        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03),
            new SectionProfile(20, CoolingRate.Slow)));

        Assert.InRange(result.GraphitizationScore, 0.0, 1.0);
        Assert.NotNull(Assert.Single(result.Flags, f => f.Code == "CHILL_RISK"));
    }

    [Fact]
    public void CoolingRate_Fast_Should_Not_Throw_And_Should_Produce_Stable_Result()
    {
        var sut = CreateSut();

        var result = sut.Estimate(new CastIronInputs(
            new CastIronComposition(3.6, 2.2, 0.3, 0.05, 0.03),
            new SectionProfile(20, CoolingRate.Fast)));

        Assert.InRange(result.GraphitizationScore, 0.0, 1.0);
        Assert.NotNull(Assert.Single(result.Flags, f => f.Code == "CHILL_RISK"));
    }
}