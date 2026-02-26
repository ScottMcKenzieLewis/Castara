using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Castara.Domain.Estimation.Validation;

namespace Castara.Domain.Tests.Estimation.Services;

/// <summary>
/// High-signal test suite for <see cref="CastIronEstimator"/>:
/// guard correctness, deterministic contract behavior, and "metallurgy-shaped" trends.
/// </summary>
/// <remarks>
/// This suite is intentionally written to:
/// - Anchor all boundary expectations to <see cref="CastIronInputConstraints"/> so UI/tests never drift from Domain.
/// - Validate exception param names (diagnostics quality).
/// - Exercise monotonic trends expected by foundry practice.
/// - Use a small, deterministic randomized sweep inside constraints (seeded) to increase confidence without flakiness.
/// </remarks>
public sealed class CastIronEstimatorTests
{
    private readonly CastIronEstimator _sut = new();

    private const double Eps = 0.0001;
    private const int SweepN = 50; // keep fast, but meaningful

    // ============================================================
    // Guard boundary tests (CompositionGuards + SectionGuards)
    // ============================================================

    public static IEnumerable<object[]> CompositionElements()
    {
        yield return new object[] { nameof(CastIronComposition.Carbon), CastIronInputConstraints.CarbonMin, CastIronInputConstraints.CarbonMax };
        yield return new object[] { nameof(CastIronComposition.Silicon), CastIronInputConstraints.SiliconMin, CastIronInputConstraints.SiliconMax };
        yield return new object[] { nameof(CastIronComposition.Manganese), CastIronInputConstraints.ManganeseMin, CastIronInputConstraints.ManganeseMax };
        yield return new object[] { nameof(CastIronComposition.Phosphorus), CastIronInputConstraints.PhosphorusMin, CastIronInputConstraints.PhosphorusMax };
        yield return new object[] { nameof(CastIronComposition.Sulfur), CastIronInputConstraints.SulfurMin, CastIronInputConstraints.SulfurMax };
    }

    [Theory]
    [MemberData(nameof(CompositionElements))]
    public void Estimate_Allows_CompositionInclusiveBoundaries(string elementName, double min, double max)
    {
        // Min
        {
            var inputs = InputsWithElement(elementName, min);
            var e = _sut.Estimate(inputs);
            Assert.NotNull(e);
        }

        // Max
        {
            var inputs = InputsWithElement(elementName, max);
            var e = _sut.Estimate(inputs);
            Assert.NotNull(e);
        }
    }

    [Theory]
    [MemberData(nameof(CompositionElements))]
    public void Estimate_Throws_ArgumentOutOfRange_ForCompositionOutOfRange(string elementName, double min, double max)
    {
        // Just below min
        {
            var inputs = InputsWithElement(elementName, min - Eps);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Estimate(inputs));
            Assert.Equal(elementName, ex.ParamName);
            Assert.Contains("Must be between", ex.Message);
        }

        // Just above max
        {
            var inputs = InputsWithElement(elementName, max + Eps);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Estimate(inputs));
            Assert.Equal(elementName, ex.ParamName);
            Assert.Contains("Must be between", ex.Message);
        }
    }

    [Fact]
    public void Estimate_Throws_ArgumentNullException_WhenInputsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _sut.Estimate(null!));
        Assert.Equal("inputs", ex.ParamName);
    }

    [Fact]
    public void Estimate_Throws_ArgumentNullException_WhenCompositionNull()
    {
        var section = new SectionProfile(ThicknessMm: 12.0, CoolingRateCPerSec: 1.0);
        var inputs = new CastIronInputs(Composition: null!, Section: section);

        var ex = Assert.Throws<ArgumentNullException>(() => _sut.Estimate(inputs));
        // If your estimator throws nameof(inputs.Composition), ParamName may be "composition" or "Composition" depending on implementation.
        // We just assert it's not empty.
        Assert.False(string.IsNullOrWhiteSpace(ex.ParamName));
    }

    [Fact]
    public void Estimate_Throws_ArgumentNullException_WhenSectionNull()
    {
        var composition = ValidComposition();
        var inputs = new CastIronInputs(Composition: composition, Section: null!);

        var ex = Assert.Throws<ArgumentNullException>(() => _sut.Estimate(inputs));
        Assert.False(string.IsNullOrWhiteSpace(ex.ParamName));
    }

    [Fact]
    public void Estimate_Throws_ArgumentException_WhenThicknessIsNonPositive()
    {
        var inputs = ValidInputs(thicknessMm: 0);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Estimate_Throws_ArgumentException_WhenThicknessIsNotFinite(double thickness)
    {
        var inputs = ValidInputs(thicknessMm: thickness);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    [Fact]
    public void Estimate_Throws_ArgumentException_WhenCoolingRateIsNonPositive()
    {
        var inputs = ValidInputs(coolingRateCPerSec: 0);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Estimate_Throws_ArgumentException_WhenCoolingRateIsNotFinite(double rate)
    {
        var inputs = ValidInputs(coolingRateCPerSec: rate);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    [Fact]
    public void Estimate_Throws_ArgumentException_WhenCoolingRateIsUnrealisticallyHigh()
    {
        var inputs = ValidInputs(coolingRateCPerSec: 200.0001);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    // ============================================================
    // Contract tests (determinism, rounding, ranges)
    // ============================================================

    [Fact]
    public void Estimate_ReturnsExpectedFlags_WithStableCodes_AndCount()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.NotNull(e);
        Assert.Equal(3, e.Flags.Count);

        var codes = e.Flags.Select(f => f.Code).ToArray();
        Assert.Contains("CHILL_RISK", codes);
        Assert.Contains("SHRINK_RISK", codes);
        Assert.Contains("MACHINABILITY", codes);
    }

    [Fact]
    public void Estimate_IsDeterministic_ForSameInputs()
    {
        var inputs = ValidInputs();

        var a = _sut.Estimate(inputs);
        var b = _sut.Estimate(inputs);

        // Scalars
        Assert.Equal(a.CarbonEquivalent, b.CarbonEquivalent);
        Assert.Equal(a.GraphitizationScore, b.GraphitizationScore);
        Assert.Equal(a.CoolingFactor, b.CoolingFactor);
        Assert.Equal(a.ThicknessFactor, b.ThicknessFactor);

        // Ranges & flags
        Assert.Equal(a.EstimatedHardness, b.EstimatedHardness);
        Assert.Equal(a.Flags, b.Flags);

        // Plot models are UI-level; not asserted here.
    }

    [Fact]
    public void Estimate_RoundsKeyScalars_ToThreeDecimals()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.Equal(Math.Round(e.CarbonEquivalent, 3), e.CarbonEquivalent);
        Assert.Equal(Math.Round(e.GraphitizationScore, 3), e.GraphitizationScore);
        Assert.Equal(Math.Round(e.CoolingFactor, 3), e.CoolingFactor);
        Assert.Equal(Math.Round(e.ThicknessFactor, 3), e.ThicknessFactor);
    }

    [Fact]
    public void Estimate_GraphitizationScore_IsAlwaysInUnitInterval()
    {
        var e = _sut.Estimate(ValidInputs());
        Assert.InRange(e.GraphitizationScore, 0.0, 1.0);
    }

    [Fact]
    public void Estimate_HardnessRange_IsOrdered()
    {
        var e = _sut.Estimate(ValidInputs());
        Assert.True(e.EstimatedHardness.MinHB <= e.EstimatedHardness.MaxHB);
    }

    // ============================================================
    // Cooling model expectations (anchors + monotonicity)
    // ============================================================

    [Theory]
    [InlineData(0.1, -0.15)]
    [InlineData(1.0, 0.00)]
    [InlineData(10.0, 0.20)]
    public void CoolingFactor_Anchors_AreRespected_AfterRounding(double rate, double expected)
    {
        var e = _sut.Estimate(ValidInputs(coolingRateCPerSec: rate));
        Assert.Equal(expected, e.CoolingFactor, 3);
    }

    [Fact]
    public void CoolingFactor_IncreasesMonotonically_WithCoolingRate()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var norm = _sut.Estimate(ValidInputs(coolingRateCPerSec: 1.0));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        Assert.True(slow.CoolingFactor < norm.CoolingFactor);
        Assert.True(norm.CoolingFactor < fast.CoolingFactor);
    }

    // ============================================================
    // "Metallurgy-shaped" trend tests (money tests)
    // ============================================================

    [Fact]
    public void FasterCooling_DecreasesGraphitization_AllElseEqual()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        Assert.True(fast.GraphitizationScore < slow.GraphitizationScore);
    }

    [Fact]
    public void FasterCooling_IncreasesHardnessMidpoint_AllElseEqual()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        Assert.True(Midpoint(fast.EstimatedHardness) > Midpoint(slow.EstimatedHardness));
    }

    [Fact]
    public void ThinnerSection_IncreasesThicknessFactor_AndDecreasesGraphitization()
    {
        var thin = _sut.Estimate(ValidInputs(thicknessMm: 5.0));
        var thick = _sut.Estimate(ValidInputs(thicknessMm: 25.0));

        Assert.True(thin.ThicknessFactor > thick.ThicknessFactor);
        Assert.True(thin.GraphitizationScore < thick.GraphitizationScore);
    }

    [Fact]
    public void ChillRiskSeverity_ShouldNotDecrease_WhenCoolingRateIncreases()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        var slowChill = GetFlag(slow, "CHILL_RISK");
        var fastChill = GetFlag(fast, "CHILL_RISK");

        Assert.True(SeverityRank(fastChill.Severity) >= SeverityRank(slowChill.Severity));
    }

    [Fact]
    public void MachinabilitySeverity_ShouldNotDecrease_WhenCoolingRateIncreases()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        var slowMach = GetFlag(slow, "MACHINABILITY");
        var fastMach = GetFlag(fast, "MACHINABILITY");

        Assert.True(SeverityRank(fastMach.Severity) >= SeverityRank(slowMach.Severity));
    }

    // ============================================================
    // “Impress the foundry” robustness sweep (seeded, non-flaky)
    // ============================================================

    [Fact]
    public void Estimate_RobustAcrossRandomValidInputs_WithinConstraints()
    {
        // Deterministic randomness: gives broad coverage without flakiness.
        var rng = new Random(20260301);

        for (int i = 0; i < SweepN; i++)
        {
            var inputs = RandomValidInputs(rng);

            var e = _sut.Estimate(inputs);

            // Fundamental invariants that must always hold for any valid input:
            Assert.InRange(e.GraphitizationScore, 0.0, 1.0);
            Assert.True(e.EstimatedHardness.MinHB <= e.EstimatedHardness.MaxHB);

            // Factors should be finite:
            Assert.True(IsFinite(e.CarbonEquivalent));
            Assert.True(IsFinite(e.CoolingFactor));
            Assert.True(IsFinite(e.ThicknessFactor));

            // Flags should be stable:
            Assert.Equal(3, e.Flags.Count);
            Assert.Contains(e.Flags, f => f.Code == "CHILL_RISK");
            Assert.Contains(e.Flags, f => f.Code == "SHRINK_RISK");
            Assert.Contains(e.Flags, f => f.Code == "MACHINABILITY");
        }
    }

    [Fact]
    public void FasterCooling_ShouldNotReduceCoolingFactor_InRandomSweep()
    {
        // “Property-style” test: for many random valid compositions & thicknesses,
        // increasing cooling rate from slow->fast should not reduce CoolingFactor.
        var rng = new Random(20260302);

        for (int i = 0; i < SweepN; i++)
        {
            var baseInputs = RandomValidInputs(rng);

            var slow = _sut.Estimate(WithCoolingRate(baseInputs, 0.1));
            var fast = _sut.Estimate(WithCoolingRate(baseInputs, 10.0));

            Assert.True(fast.CoolingFactor >= slow.CoolingFactor);
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static CastIronInputs WithCoolingRate(CastIronInputs inputs, double coolingRate)
    {
        var c = inputs.Composition;
        var s = inputs.Section;

        var section = new SectionProfile(
            ThicknessMm: s.ThicknessMm,
            CoolingRateCPerSec: coolingRate);

        return new CastIronInputs(Composition: c, Section: section);
    }

    private static CastIronInputs InputsWithElement(string elementName, double value)
        => elementName switch
        {
            nameof(CastIronComposition.Carbon) => ValidInputs(carbon: value),
            nameof(CastIronComposition.Silicon) => ValidInputs(silicon: value),
            nameof(CastIronComposition.Manganese) => ValidInputs(manganese: value),
            nameof(CastIronComposition.Phosphorus) => ValidInputs(phosphorus: value),
            nameof(CastIronComposition.Sulfur) => ValidInputs(sulfur: value),
            _ => throw new InvalidOperationException("Bad test setup.")
        };

    private static CastIronComposition ValidComposition(
        double carbon = 3.40,
        double silicon = 2.10,
        double manganese = 0.55,
        double phosphorus = 0.05,
        double sulfur = 0.02)
        => new(
            Carbon: carbon,
            Silicon: silicon,
            Manganese: manganese,
            Phosphorus: phosphorus,
            Sulfur: sulfur);

    private static CastIronInputs ValidInputs(
        double carbon = 3.40,
        double silicon = 2.10,
        double manganese = 0.55,
        double phosphorus = 0.05,
        double sulfur = 0.02,
        double thicknessMm = 12.0,
        double coolingRateCPerSec = 1.0)
    {
        var composition = new CastIronComposition(
            Carbon: carbon,
            Silicon: silicon,
            Manganese: manganese,
            Phosphorus: phosphorus,
            Sulfur: sulfur);

        var section = new SectionProfile(
            ThicknessMm: thicknessMm,
            CoolingRateCPerSec: coolingRateCPerSec);

        return new CastIronInputs(Composition: composition, Section: section);
    }

    private static CastIronInputs RandomValidInputs(Random rng)
    {
        // Choose values comfortably inside valid ranges to avoid incidental boundary hits.
        // (Boundaries are already tested explicitly above.)
        double carbon = NextInRange(rng, CastIronInputConstraints.CarbonMin + 0.05, CastIronInputConstraints.CarbonMax - 0.05);
        double silicon = NextInRange(rng, CastIronInputConstraints.SiliconMin + 0.05, CastIronInputConstraints.SiliconMax - 0.05);
        double manganese = NextInRange(rng, CastIronInputConstraints.ManganeseMin + 0.01, CastIronInputConstraints.ManganeseMax - 0.01);
        double phosphorus = NextInRange(rng, CastIronInputConstraints.PhosphorusMin + 0.005, CastIronInputConstraints.PhosphorusMax - 0.005);
        double sulfur = NextInRange(rng, CastIronInputConstraints.SulfurMin + 0.002, CastIronInputConstraints.SulfurMax - 0.002);

        // Section: keep realistic and within SectionGuards
        double thickness = NextInRange(rng, 3.0, 60.0);          // mm
        double cooling = NextInRange(rng, 0.1, 25.0);          // °C/s (well below 200 guard)

        return ValidInputs(
            carbon: carbon,
            silicon: silicon,
            manganese: manganese,
            phosphorus: phosphorus,
            sulfur: sulfur,
            thicknessMm: thickness,
            coolingRateCPerSec: cooling);
    }

    private static double NextInRange(Random rng, double min, double max)
    {
        if (min >= max) throw new ArgumentException("Bad test range.");
        return min + (rng.NextDouble() * (max - min));
    }

    private static RiskFlag GetFlag(CastIronEstimate estimate, string code)
        => estimate.Flags.Single(f => string.Equals(f.Code, code, StringComparison.Ordinal));

    private static int SeverityRank(RiskSeverity s)
        => s switch
        {
            RiskSeverity.Low => 0,
            RiskSeverity.Medium => 1,
            RiskSeverity.High => 2,
            _ => -1
        };

    private static double Midpoint(HardnessRange r)
        => (r.MinHB + r.MaxHB) / 2.0;

    private static bool IsFinite(double x)
        => !(double.IsNaN(x) || double.IsInfinity(x));
}