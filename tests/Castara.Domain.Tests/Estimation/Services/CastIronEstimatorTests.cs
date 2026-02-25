using System;
using System.Linq;
using Xunit;
using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;

namespace Castara.Domain.Tests.Estimation.Services;

/// <summary>
/// Contains unit tests for <see cref="CastIronEstimator"/> to verify metallurgical
/// calculation correctness, input validation, and expected behavioral trends.
/// </summary>
/// <remarks>
/// <para>
/// This test suite validates the cast iron estimation engine through multiple testing strategies:
/// </para>
/// <para>
/// <strong>Guard Boundary Tests:</strong> Verify input validation at composition and section boundaries
/// using <see cref="CompositionGuards"/> and <see cref="SectionGuards"/>.
/// </para>
/// <para>
/// <strong>Contract Tests:</strong> Ensure determinism, proper rounding, and value ranges for
/// calculated properties across multiple invocations.
/// </para>
/// <para>
/// <strong>Cooling Model Tests:</strong> Validate logarithmic cooling rate interpolation against
/// known anchor points and verify monotonic behavior.
/// </para>
/// <para>
/// <strong>Metallurgical Trend Tests:</strong> The critical "money tests" that verify the estimator
/// produces physically correct trends (e.g., faster cooling decreases graphitization, increases hardness).
/// </para>
/// </remarks>
public sealed class CastIronEstimatorTests
{
    /// <summary>
    /// The system under test (SUT) - an instance of the cast iron estimator.
    /// </summary>
    private readonly CastIronEstimator _sut = new();

    // ============================================================
    // Guard boundary tests (CompositionGuards + SectionGuards)
    // ============================================================

    /// <summary>
    /// Verifies that the estimator accepts composition values at inclusive boundary limits
    /// defined by <see cref="CompositionGuards"/>.
    /// </summary>
    /// <param name="c">Carbon percentage (2.5-4.5 wt%).</param>
    /// <param name="si">Silicon percentage (0.5-3.5 wt%).</param>
    /// <param name="mn">Manganese percentage (0.0-1.5 wt%).</param>
    /// <param name="p">Phosphorus percentage (0.0-0.3 wt%).</param>
    /// <param name="s">Sulfur percentage (0.0-0.2 wt%).</param>
    /// <remarks>
    /// Each test case validates one element at its boundary limits while keeping
    /// others at typical values, ensuring inclusive boundary acceptance.
    /// </remarks>
    [Theory]
    // Carbon: 2.5 - 4.5
    [InlineData(2.5, 2.0, 0.5, 0.0, 0.0)]
    [InlineData(4.5, 2.0, 0.5, 0.0, 0.0)]
    // Silicon: 0.5 - 3.5
    [InlineData(3.4, 0.5, 0.5, 0.0, 0.0)]
    [InlineData(3.4, 3.5, 0.5, 0.0, 0.0)]
    // Manganese: 0.0 - 1.5
    [InlineData(3.4, 2.0, 0.0, 0.0, 0.0)]
    [InlineData(3.4, 2.0, 1.5, 0.0, 0.0)]
    // Phosphorus: 0.0 - 0.3
    [InlineData(3.4, 2.0, 0.5, 0.0, 0.0)]
    [InlineData(3.4, 2.0, 0.5, 0.3, 0.0)]
    // Sulfur: 0.0 - 0.2
    [InlineData(3.4, 2.0, 0.5, 0.0, 0.2)]
    public void Estimate_Allows_CompositionInclusiveBoundaries(
        double c, double si, double mn, double p, double s)
    {
        var inputs = ValidInputs(carbon: c, silicon: si, manganese: mn, phosphorus: p, sulfur: s);

        var e = _sut.Estimate(inputs);

        Assert.NotNull(e);
    }

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentOutOfRangeException"/> when
    /// composition values fall outside the valid ranges defined by <see cref="CompositionGuards"/>.
    /// </summary>
    /// <param name="badValue">The out-of-range value to test.</param>
    /// <param name="elementName">The name of the element being tested (e.g., "Carbon", "Silicon").</param>
    /// <remarks>
    /// Tests values just outside the boundaries (±0.0001) to verify exclusive boundary behavior.
    /// Also validates that the exception's parameter name matches the element name for clear diagnostics.
    /// </remarks>
    [Theory]
    // Carbon
    [InlineData(2.4999, "Carbon")]
    [InlineData(4.5001, "Carbon")]
    // Silicon
    [InlineData(0.4999, "Silicon")]
    [InlineData(3.5001, "Silicon")]
    // Manganese
    [InlineData(-0.0001, "Manganese")]
    [InlineData(1.5001, "Manganese")]
    // Phosphorus
    [InlineData(-0.0001, "Phosphorus")]
    [InlineData(0.3001, "Phosphorus")]
    // Sulfur
    [InlineData(-0.0001, "Sulfur")]
    [InlineData(0.2001, "Sulfur")]
    public void Estimate_Throws_ArgumentOutOfRange_ForCompositionOutOfRange(double badValue, string elementName)
    {
        var inputs = elementName switch
        {
            "Carbon" => ValidInputs(carbon: badValue),
            "Silicon" => ValidInputs(silicon: badValue),
            "Manganese" => ValidInputs(manganese: badValue),
            "Phosphorus" => ValidInputs(phosphorus: badValue),
            "Sulfur" => ValidInputs(sulfur: badValue),
            _ => throw new InvalidOperationException("Bad test setup.")
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Estimate(inputs));

        // Verify that RequireRange uses nameof(c.X) for accurate error messages
        Assert.Equal(elementName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentException"/> when section
    /// thickness is non-positive (zero or negative).
    /// </summary>
    [Fact]
    public void Estimate_Throws_ArgumentException_WhenThicknessIsNonPositive()
    {
        var inputs = ValidInputs(thicknessMm: 0);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentException"/> when section
    /// thickness is not a finite number (NaN or Infinity).
    /// </summary>
    /// <param name="thickness">The non-finite thickness value to test.</param>
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

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentException"/> when cooling
    /// rate is non-positive (zero or negative).
    /// </summary>
    [Fact]
    public void Estimate_Throws_ArgumentException_WhenCoolingRateIsNonPositive()
    {
        var inputs = ValidInputs(coolingRateCPerSec: 0);

        var ex = Assert.Throws<ArgumentException>(() => _sut.Estimate(inputs));
        Assert.Equal("section", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentException"/> when cooling
    /// rate is not a finite number (NaN or Infinity).
    /// </summary>
    /// <param name="rate">The non-finite cooling rate value to test.</param>
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

    /// <summary>
    /// Verifies that the estimator throws <see cref="ArgumentException"/> when cooling
    /// rate exceeds the realistic upper limit (200 °C/s) for casting operations.
    /// </summary>
    /// <remarks>
    /// This sanity check helps catch unit conversion errors or data entry mistakes.
    /// </remarks>
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

    /// <summary>
    /// Verifies that the estimator returns exactly three risk flags with stable,
    /// expected flag codes.
    /// </summary>
    /// <remarks>
    /// This ensures the risk assessment system produces consistent flag structures
    /// regardless of input values. The three flags are: CHILL_RISK, SHRINK_RISK, and MACHINABILITY.
    /// </remarks>
    [Fact]
    public void Estimate_ReturnsExpectedFlags_WithStableCodes_AndCount()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.Equal(3, e.Flags.Count);

        var codes = e.Flags.Select(f => f.Code).ToArray();
        Assert.Contains("CHILL_RISK", codes);
        Assert.Contains("SHRINK_RISK", codes);
        Assert.Contains("MACHINABILITY", codes);
    }

    /// <summary>
    /// Verifies that the estimator produces identical results for identical inputs,
    /// ensuring deterministic behavior without hidden state or randomness.
    /// </summary>
    /// <remarks>
    /// Determinism is critical for reliable predictions and debugging. This test
    /// validates all output properties including flags.
    /// </remarks>
    [Fact]
    public void Estimate_IsDeterministic_ForSameInputs()
    {
        var inputs = ValidInputs();

        var a = _sut.Estimate(inputs);
        var b = _sut.Estimate(inputs);

        Assert.Equal(a.CarbonEquivalent, b.CarbonEquivalent);
        Assert.Equal(a.GraphitizationScore, b.GraphitizationScore);
        Assert.Equal(a.CoolingFactor, b.CoolingFactor);
        Assert.Equal(a.ThicknessFactor, b.ThicknessFactor);
        Assert.Equal(a.EstimatedHardness, b.EstimatedHardness);
        Assert.Equal(a.Flags, b.Flags);
    }

    /// <summary>
    /// Verifies that key scalar outputs are rounded to exactly three decimal places.
    /// </summary>
    /// <remarks>
    /// Consistent rounding ensures predictable display formatting and prevents
    /// floating-point precision artifacts from appearing in the UI.
    /// </remarks>
    [Fact]
    public void Estimate_RoundsKeyScalars_ToThreeDecimals()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.Equal(Math.Round(e.CarbonEquivalent, 3), e.CarbonEquivalent);
        Assert.Equal(Math.Round(e.GraphitizationScore, 3), e.GraphitizationScore);
        Assert.Equal(Math.Round(e.CoolingFactor, 3), e.CoolingFactor);
        Assert.Equal(Math.Round(e.ThicknessFactor, 3), e.ThicknessFactor);
    }

    /// <summary>
    /// Verifies that the graphitization score is always within the valid unit interval [0, 1].
    /// </summary>
    /// <remarks>
    /// The graphitization score represents a normalized tendency, so it must be clamped
    /// to prevent invalid visualization states or calculation errors downstream.
    /// </remarks>
    [Fact]
    public void Estimate_GraphitizationScore_IsAlwaysInUnitInterval()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.InRange(e.GraphitizationScore, 0.0, 1.0);
    }

    /// <summary>
    /// Verifies that the hardness range is properly ordered (MinHB ≤ MaxHB).
    /// </summary>
    /// <remarks>
    /// An inverted range would indicate a calculation error or incorrect spread application.
    /// </remarks>
    [Fact]
    public void Estimate_HardnessRange_IsOrdered()
    {
        var e = _sut.Estimate(ValidInputs());

        Assert.True(e.EstimatedHardness.MinHB <= e.EstimatedHardness.MaxHB);
    }

    // ============================================================
    // Cooling model expectations (anchors + monotonicity)
    // ============================================================

    /// <summary>
    /// Verifies that the cooling factor calculation produces expected values at
    /// three anchor points: slow (0.1 °C/s), normal (1.0 °C/s), and fast (10.0 °C/s).
    /// </summary>
    /// <param name="rate">The cooling rate in °C/s.</param>
    /// <param name="expected">The expected cooling factor at this rate.</param>
    /// <remarks>
    /// These anchor points define the logarithmic interpolation model. Deviations
    /// indicate changes to the cooling factor calculation algorithm.
    /// </remarks>
    [Theory]
    [InlineData(0.1, -0.15)]
    [InlineData(1.0, 0.00)]
    [InlineData(10.0, 0.20)]
    public void CoolingFactor_Anchors_AreRespected_AfterRounding(double rate, double expected)
    {
        var e = _sut.Estimate(ValidInputs(coolingRateCPerSec: rate));

        Assert.Equal(expected, e.CoolingFactor, 3);
    }

    /// <summary>
    /// Verifies that the cooling factor increases monotonically as cooling rate increases.
    /// </summary>
    /// <remarks>
    /// Monotonicity ensures the logarithmic interpolation behaves correctly between anchor
    /// points and prevents physically impossible inversions.
    /// </remarks>
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
    // "Metallurgy-shaped" trend tests (these are the money tests)
    // ============================================================

    /// <summary>
    /// Verifies that faster cooling rates decrease graphitization score, reflecting
    /// the metallurgical principle that rapid cooling inhibits graphite formation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Metallurgical Basis:</strong> Faster cooling reduces the time available
    /// for carbon atoms to diffuse and form graphite nuclei, favoring carbide formation instead.
    /// </para>
    /// <para>
    /// This is a critical "money test" - failure indicates the model contradicts
    /// fundamental metallurgical behavior.
    /// </para>
    /// </remarks>
    [Fact]
    public void FasterCooling_DecreasesGraphitization_AllElseEqual()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        Assert.True(fast.GraphitizationScore < slow.GraphitizationScore);
    }

    /// <summary>
    /// Verifies that faster cooling rates increase hardness, reflecting the metallurgical
    /// principle that rapid cooling produces harder structures (finer pearlite, more carbides).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Metallurgical Basis:</strong> Faster cooling produces finer microstructures
    /// with increased carbide content, both of which increase hardness.
    /// </para>
    /// <para>
    /// This is a critical "money test" - failure indicates the model contradicts
    /// fundamental metallurgical behavior.
    /// </para>
    /// </remarks>
    [Fact]
    public void FasterCooling_IncreasesHardnessMidpoint_AllElseEqual()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        Assert.True(Midpoint(fast.EstimatedHardness) > Midpoint(slow.EstimatedHardness));
    }

    /// <summary>
    /// Verifies that thinner sections (which cool faster) have higher thickness factors
    /// and lower graphitization scores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Metallurgical Basis:</strong> Thinner sections have higher surface-to-volume
    /// ratios, causing faster heat dissipation and reduced graphitization.
    /// </para>
    /// <para>
    /// The thickness factor increases for thinner sections because it represents deviation
    /// from a reference thickness in a direction that promotes faster cooling.
    /// </para>
    /// </remarks>
    [Fact]
    public void ThinnerSection_IncreasesThicknessFactor_AndDecreasesGraphitization()
    {
        var thin = _sut.Estimate(ValidInputs(thicknessMm: 5.0));
        var thick = _sut.Estimate(ValidInputs(thicknessMm: 25.0));

        Assert.True(thin.ThicknessFactor > thick.ThicknessFactor);
        Assert.True(thin.GraphitizationScore < thick.GraphitizationScore);
    }

    /// <summary>
    /// Verifies that chill risk severity does not decrease when cooling rate increases.
    /// </summary>
    /// <remarks>
    /// Faster cooling increases the likelihood of carbide (chill) formation, so risk
    /// severity should increase or remain constant, never decrease.
    /// </remarks>
    [Fact]
    public void ChillRiskSeverity_ShouldNotDecrease_WhenCoolingRateIncreases()
    {
        var slow = _sut.Estimate(ValidInputs(coolingRateCPerSec: 0.1));
        var fast = _sut.Estimate(ValidInputs(coolingRateCPerSec: 10.0));

        var slowChill = GetFlag(slow, "CHILL_RISK");
        var fastChill = GetFlag(fast, "CHILL_RISK");

        Assert.True(SeverityRank(fastChill.Severity) >= SeverityRank(slowChill.Severity));
    }

    /// <summary>
    /// Verifies that machinability concern severity does not decrease when cooling rate increases.
    /// </summary>
    /// <remarks>
    /// Faster cooling produces harder structures that are more difficult to machine,
    /// so machinability risk severity should increase or remain constant, never decrease.
    /// </remarks>
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
    // Helpers
    // ============================================================

    /// <summary>
    /// Creates valid <see cref="CastIronInputs"/> with typical gray iron composition
    /// and section parameters, allowing selective override of specific values.
    /// </summary>
    /// <param name="carbon">Carbon percentage (default: 3.40 wt%).</param>
    /// <param name="silicon">Silicon percentage (default: 2.10 wt%).</param>
    /// <param name="manganese">Manganese percentage (default: 0.55 wt%).</param>
    /// <param name="phosphorus">Phosphorus percentage (default: 0.05 wt%).</param>
    /// <param name="sulfur">Sulfur percentage (default: 0.02 wt%).</param>
    /// <param name="thicknessMm">Section thickness in mm (default: 12.0 mm).</param>
    /// <param name="coolingRateCPerSec">Cooling rate in °C/s (default: 1.0 °C/s).</param>
    /// <returns>A <see cref="CastIronInputs"/> instance with specified or default values.</returns>
    /// <remarks>
    /// Default values represent a typical Class 30 gray iron composition with
    /// moderate section thickness and normal cooling conditions.
    /// </remarks>
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

    /// <summary>
    /// Retrieves a specific risk flag from an estimate by its code.
    /// </summary>
    /// <param name="estimate">The estimate containing risk flags.</param>
    /// <param name="code">The flag code to retrieve (e.g., "CHILL_RISK").</param>
    /// <returns>The matching <see cref="RiskFlag"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the flag with the specified code is not found or multiple flags match.
    /// </exception>
    private static RiskFlag GetFlag(CastIronEstimate estimate, string code)
        => estimate.Flags.Single(f => string.Equals(f.Code, code, StringComparison.Ordinal));

    /// <summary>
    /// Converts a <see cref="RiskSeverity"/> enum value to a numeric rank for comparison.
    /// </summary>
    /// <param name="s">The severity level.</param>
    /// <returns>An integer rank: 0 (Low), 1 (Medium), 2 (High), or -1 (unknown).</returns>
    private static int SeverityRank(RiskSeverity s)
        => s switch
        {
            RiskSeverity.Low => 0,
            RiskSeverity.Medium => 1,
            RiskSeverity.High => 2,
            _ => -1
        };

    /// <summary>
    /// Calculates the midpoint of a hardness range.
    /// </summary>
    /// <param name="r">The hardness range.</param>
    /// <returns>The average of MinHB and MaxHB.</returns>
    private static double Midpoint(HardnessRange r)
        => (r.MinHB + r.MaxHB) / 2.0;
}