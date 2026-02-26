using System;
using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Castara.Domain.Estimation.Validation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Castara.Domain.Tests.Estimation.Services;

/// <summary>
/// Property-based tests for <see cref="CastIronEstimator"/>.
//
// These tests verify estimator invariants across a wide range
// of automatically generated valid metallurgical inputs.
//
// This complements deterministic unit tests by stress-testing:
//
// • Numerical stability
// • Physical invariants
// • Guard correctness
// • Output constraints
//
// This style of testing is common in engineering-grade software
// where calculations must remain stable across a large input space.
// </summary>
public sealed class CastIronEstimatorPropertyTests
{
    private readonly CastIronEstimator _sut = new();

    // ============================================================
    // Arbitrary Generators
    // ============================================================

    /// <summary>
    /// Generates arbitrary <see cref="CastIronInputs"/> instances
    /// within the valid constraint boundaries defined in <see cref="CastIronInputConstraints"/>.
    /// </summary>
    /// <returns>An arbitrary generator for valid cast iron inputs.</returns>
    public static Arbitrary<CastIronInputs> ValidInputs()
    {
        var gen =
            from c in Gen.Choose(0, 1000).Select(x =>
                CastIronInputConstraints.CarbonMin +
                x / 1000.0 *
                (CastIronInputConstraints.CarbonMax -
                 CastIronInputConstraints.CarbonMin))

            from si in Gen.Choose(0, 1000).Select(x =>
                CastIronInputConstraints.SiliconMin +
                x / 1000.0 *
                (CastIronInputConstraints.SiliconMax -
                 CastIronInputConstraints.SiliconMin))

            from mn in Gen.Choose(0, 1000).Select(x =>
                CastIronInputConstraints.ManganeseMin +
                x / 1000.0 *
                (CastIronInputConstraints.ManganeseMax -
                 CastIronInputConstraints.ManganeseMin))

            from p in Gen.Choose(0, 1000).Select(x =>
                CastIronInputConstraints.PhosphorusMin +
                x / 1000.0 *
                (CastIronInputConstraints.PhosphorusMax -
                 CastIronInputConstraints.PhosphorusMin))

            from s in Gen.Choose(0, 1000).Select(x =>
                CastIronInputConstraints.SulfurMin +
                x / 1000.0 *
                (CastIronInputConstraints.SulfurMax -
                 CastIronInputConstraints.SulfurMin))

            from thickness in Gen.Choose(1, 100)
            from cooling in Gen.Choose(1, 100)

            select new CastIronInputs(
                new CastIronComposition(c, si, mn, p, s),
                new SectionProfile(thickness, cooling));

        return gen.ToArbitrary();
    }

    // ============================================================
    // Core invariants
    // ============================================================

    /// <summary>
    /// Graphitization score must always be in [0,1].
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void GraphitizationScore_IsAlwaysUnitInterval(CastIronInputs inputs)
    {
        var e = _sut.Estimate(inputs);

        Assert.InRange(e.GraphitizationScore, 0.0, 1.0);
    }

    /// <summary>
    /// Hardness range must always be ordered.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void HardnessRange_IsAlwaysOrdered(CastIronInputs inputs)
    {
        var e = _sut.Estimate(inputs);

        Assert.True(e.EstimatedHardness.MinHB <= e.EstimatedHardness.MaxHB);
    }

    /// <summary>
    /// Carbon equivalent must be within bounds derived from input constraints.
    /// CE = C + (Si + P)/3 should respect the min/max of each component.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void CarbonEquivalent_IsWithinConstraintDerivedBounds(CastIronInputs inputs)
    {
        var e = _sut.Estimate(inputs);

        // CE = C + (Si + P)/3
        var minCe =
            CastIronInputConstraints.CarbonMin
            + (CastIronInputConstraints.SiliconMin + CastIronInputConstraints.PhosphorusMin) / 3.0;

        var maxCe =
            CastIronInputConstraints.CarbonMax
            + (CastIronInputConstraints.SiliconMax + CastIronInputConstraints.PhosphorusMax) / 3.0;

        // Estimator rounds to 3 decimals when returning the estimate
        // but CarbonEquivalent itself in the estimate is already rounded.
        Assert.InRange(e.CarbonEquivalent, Math.Round(minCe, 3), Math.Round(maxCe, 3));
    }

    /// <summary>
    /// Estimator must be deterministic.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void Estimator_IsDeterministic(CastIronInputs inputs)
    {
        var a = _sut.Estimate(inputs);
        var b = _sut.Estimate(inputs);

        Assert.Equal(a.CarbonEquivalent, b.CarbonEquivalent);
        Assert.Equal(a.GraphitizationScore, b.GraphitizationScore);
        Assert.Equal(a.CoolingFactor, b.CoolingFactor);
        Assert.Equal(a.ThicknessFactor, b.ThicknessFactor);
        Assert.Equal(a.EstimatedHardness, b.EstimatedHardness);
    }

    // ============================================================
    // Physical metallurgy invariants
    // ============================================================

    /// <summary>
    /// Increasing cooling rate must not increase graphitization.
    /// </summary>
    [Property]
    public void FasterCooling_DecreasesGraphitization(
        double rate1,
        double rate2)
    {
        rate1 = Clamp(rate1, 0.1, 50);
        rate2 = Clamp(rate2, 0.1, 50);

        if (rate2 <= rate1)
            return;

        var baseInputs = TypicalInputs(rate1);
        var fastInputs = TypicalInputs(rate2);

        var slow = _sut.Estimate(baseInputs);
        var fast = _sut.Estimate(fastInputs);

        Assert.True(
            fast.GraphitizationScore <= slow.GraphitizationScore);
    }

    /// <summary>
    /// Faster cooling should not reduce hardness.
    /// </summary>
    [Property]
    public void FasterCooling_IncreasesHardness(
        double rate1,
        double rate2)
    {
        rate1 = Clamp(rate1, 0.1, 50);
        rate2 = Clamp(rate2, 0.1, 50);

        if (rate2 <= rate1)
            return;

        var slow = _sut.Estimate(TypicalInputs(rate1));
        var fast = _sut.Estimate(TypicalInputs(rate2));

        Assert.True(
            Midpoint(fast.EstimatedHardness) >=
            Midpoint(slow.EstimatedHardness));
    }

    // ============================================================
    // Numerical stability
    // ============================================================

    /// <summary>
    /// Estimator must never throw for valid inputs.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void Estimator_NeverThrows_ForValidInputs(
        CastIronInputs inputs)
    {
        var e = _sut.Estimate(inputs);

        Assert.NotNull(e);
    }

    /// <summary>
    /// Estimator must never produce NaN or Infinity.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CastIronEstimatorPropertyTests) })]
    public void Outputs_AreAlwaysFinite(CastIronInputs inputs)
    {
        var e = _sut.Estimate(inputs);

        Assert.True(double.IsFinite(e.CarbonEquivalent));
        Assert.True(double.IsFinite(e.GraphitizationScore));
        Assert.True(double.IsFinite(e.CoolingFactor));
        Assert.True(double.IsFinite(e.ThicknessFactor));
    }

    // ============================================================
    // Helpers
    // ============================================================

    /// <summary>
    /// Creates typical cast iron inputs with the specified cooling rate.
    /// Uses a standard composition (C: 3.4%, Si: 2.1%, Mn: 0.55%, P: 0.05%, S: 0.02%).
    /// </summary>
    /// <param name="coolingRate">The cooling rate in °C/s.</param>
    /// <returns>A typical cast iron input configuration.</returns>
    private static CastIronInputs TypicalInputs(double coolingRate)
        => new(
            new CastIronComposition(
                3.4,
                2.1,
                0.55,
                0.05,
                0.02),
            new SectionProfile(
                12,
                coolingRate));

    /// <summary>
    /// Clamps a value to the specified range, handling NaN and infinity.
    /// </summary>
    /// <param name="v">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value within [min, max].</returns>
    private static double Clamp(double v, double min, double max)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
            return min;

        if (v < min) return min;
        if (v > max) return max;

        return v;
    }

    /// <summary>
    /// Calculates the midpoint of a hardness range.
    /// </summary>
    /// <param name="r">The hardness range.</param>
    /// <returns>The average of the minimum and maximum hardness values.</returns>
    private static double Midpoint(HardnessRange r)
        => (r.MinHB + r.MaxHB) / 2.0;
}