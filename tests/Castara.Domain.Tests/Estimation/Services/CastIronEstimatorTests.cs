using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Models.Outputs;
using Castara.Domain.Estimation.Services;
using Castara.Domain.Estimation.Services.Strategies;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace Castara.Domain.Tests.Estimation.Services;

public sealed class CastIronEstimatorTests
{
    [Fact]
    public void Constructor_WhenStrategiesIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new CastIronEstimator(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategies");
    }

    [Fact]
    public void Estimate_WhenInputsIsNull_ThrowsArgumentNullException()
    {
        var strategy = new Mock<ICastingEstimatorStrategy>();
        var sut = new CastIronEstimator([strategy.Object]);
        var profile = CreateGeneralGrayIronProfile();

        Action act = () => sut.Estimate(null!, profile);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inputs");
    }

    [Fact]
    public void Estimate_WhenProfileIsNull_ThrowsArgumentNullException()
    {
        var strategy = new Mock<ICastingEstimatorStrategy>();
        var sut = new CastIronEstimator([strategy.Object]);
        var inputs = CreateValidInputs();

        Action act = () => sut.Estimate(inputs, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void Estimate_WhenNoCompatibleStrategyExists_ThrowsInvalidOperationException()
    {
        var strategy = new Mock<ICastingEstimatorStrategy>(MockBehavior.Strict);
        strategy.Setup(x => x.CanHandle(It.IsAny<CastingProfileDefinition>()))
            .Returns(false);

        var sut = new CastIronEstimator([strategy.Object]);
        var inputs = CreateValidInputs();
        var profile = CreateGeneralGrayIronProfile();

        Action act = () => sut.Estimate(inputs, profile);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{profile.Id}*")
            .WithMessage("*compatible strategy*");
    }

    [Fact]
    public void Estimate_WhenCompatibleStrategyExists_ReturnsDelegatedResult()
    {
        var expected = CreateExpectedEstimate();

        var strategy = new Mock<ICastingEstimatorStrategy>(MockBehavior.Strict);
        strategy.Setup(x => x.CanHandle(It.IsAny<CastingProfileDefinition>()))
            .Returns(true);
        strategy.Setup(x => x.Estimate(It.IsAny<CastIronInputs>(), It.IsAny<CastingProfileDefinition>()))
            .Returns(expected);

        var sut = new CastIronEstimator([strategy.Object]);
        var inputs = CreateValidInputs();
        var profile = CreateGeneralGrayIronProfile();

        var actual = sut.Estimate(inputs, profile);

        actual.Should().BeSameAs(expected);
    }

    [Fact]
    public void Estimate_PassesInputsAndProfileToSelectedStrategy()
    {
        var expected = CreateExpectedEstimate();
        var inputs = CreateValidInputs();
        var profile = CreateGeneralGrayIronProfile();

        var strategy = new Mock<ICastingEstimatorStrategy>(MockBehavior.Strict);
        strategy.Setup(x => x.CanHandle(profile)).Returns(true);
        strategy.Setup(x => x.Estimate(inputs, profile)).Returns(expected);

        var sut = new CastIronEstimator([strategy.Object]);

        var actual = sut.Estimate(inputs, profile);

        actual.Should().BeSameAs(expected);

        strategy.Verify(x => x.CanHandle(profile), Times.Once);
        strategy.Verify(x => x.Estimate(inputs, profile), Times.Once);
    }

    [Fact]
    public void Estimate_WhenMultipleStrategiesExist_UsesFirstCompatibleStrategy()
    {
        var inputs = CreateValidInputs();
        var profile = CreateGeneralGrayIronProfile();
        var expected = CreateExpectedEstimate();

        var first = new Mock<ICastingEstimatorStrategy>(MockBehavior.Strict);
        first.Setup(x => x.CanHandle(profile)).Returns(true);
        first.Setup(x => x.Estimate(inputs, profile)).Returns(expected);

        var second = new Mock<ICastingEstimatorStrategy>(MockBehavior.Strict);
        second.Setup(x => x.CanHandle(profile)).Returns(true);

        var sut = new CastIronEstimator([first.Object, second.Object]);

        var actual = sut.Estimate(inputs, profile);

        actual.Should().BeSameAs(expected);

        first.Verify(x => x.Estimate(inputs, profile), Times.Once);
        second.Verify(x => x.Estimate(It.IsAny<CastIronInputs>(), It.IsAny<CastingProfileDefinition>()), Times.Never);
    }

    private static CastIronInputs CreateValidInputs()
        => new(
            Composition: new CastIronComposition(
                Carbon: 3.40,
                Silicon: 2.10,
                Manganese: 0.55,
                Phosphorus: 0.05,
                Sulfur: 0.02),
            Section: new SectionProfile(
                ThicknessMm: 12.0,
                CoolingRateCPerSec: 1.0));

    private static CastIronEstimate CreateExpectedEstimate()
        => new(
            CarbonEquivalent: 4.10,
            GraphitizationScore: 0.72,
            EstimatedHardness: new HardnessRange(190, 230),
            CoolingFactor: 1.10,
            ThicknessFactor: 0.85,
            Flags: []);

    private static CastingProfileDefinition CreateGeneralGrayIronProfile()
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
}