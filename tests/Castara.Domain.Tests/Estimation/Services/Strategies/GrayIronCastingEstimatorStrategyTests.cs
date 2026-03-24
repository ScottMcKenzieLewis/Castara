using Castara.Domain.Composition;
using Castara.Domain.Estimation.Models.Inputs;
using Castara.Domain.Estimation.Services.Strategies;
using Castara.Domain.Exceptions;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Castara.Domain.Tests.Estimation.Strategies;

public sealed class GrayIronCastingEstimatorStrategyTests
{
    private readonly GrayIronCastingEstimatorStrategy _sut = new();

    [Fact]
    public void CanHandle_WhenProfileIsGrayIron_ReturnsTrue()
    {
        var profile = CreateGeneralGrayIronProfile();

        var result = _sut.CanHandle(profile);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WhenProfileIsNotGrayIron_ReturnsFalse()
    {
        var profile = CreateDuctileIronProfile();

        var result = _sut.CanHandle(profile);

        result.Should().BeFalse();
    }

    [Fact]
    public void Estimate_WhenInputsIsNull_ThrowsArgumentNullException()
    {
        var profile = CreateGeneralGrayIronProfile();

        Action act = () => _sut.Estimate(null!, profile);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inputs");
    }

    [Fact]
    public void Estimate_WhenProfileIsNull_ThrowsArgumentNullException()
    {
        var inputs = CreateValidInputs();

        Action act = () => _sut.Estimate(inputs, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void Estimate_WhenProfileDefinitionIsInvalid_ThrowsDomainException()
    {
        var inputs = CreateValidInputs();

        var invalidProfile = CreateGeneralGrayIronProfile() with
        {
            ShrinkageRiskFloor = 4.5,
            ChillRiskCeiling = 4.4
        };

        Action act = () => _sut.Estimate(inputs, invalidProfile);

        act.Should().Throw<DomainException>()
            .WithMessage("*must not exceed*");
    }

    [Fact]
    public void Estimate_WhenCompositionViolatesGlobalConstraints_ThrowsDomainException()
    {
        var inputs = new CastIronInputs(
            Composition: new CastIronComposition(
                Carbon: 99.0,
                Silicon: 2.10,
                Manganese: 0.55,
                Phosphorus: 0.05,
                Sulfur: 0.02),
            Section: new SectionProfile(
                ThicknessMm: 12.0,
                CoolingRateCPerSec: 1.0));

        var profile = CreateGeneralGrayIronProfile();

        Action act = () => _sut.Estimate(inputs, profile);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Estimate_WithValidInputs_ReturnsEstimate()
    {
        var inputs = CreateValidInputs();
        var profile = CreateGeneralGrayIronProfile();

        var result = _sut.Estimate(inputs, profile);

        result.Should().NotBeNull();
        result.CarbonEquivalent.Should().BePositive();
        result.GraphitizationScore.Should().BeGreaterThan(0);
        result.EstimatedHardness.MinHB.Should().BePositive();
        result.EstimatedHardness.MaxHB.Should().BeGreaterThanOrEqualTo(result.EstimatedHardness.MinHB);
        result.Flags.Should().NotBeNull();
    }

    [Fact]
    public void Estimate_GeneralVsThinSection_ProducesDifferentResult()
    {
        var inputs = CreateValidInputs();
        var general = CreateGeneralGrayIronProfile();
        var thin = CreateThinSectionGrayIronProfile();

        var generalResult = _sut.Estimate(inputs, general);
        var thinResult = _sut.Estimate(inputs, thin);

        var differs =
            generalResult.GraphitizationScore != thinResult.GraphitizationScore
            || generalResult.CoolingFactor != thinResult.CoolingFactor
            || generalResult.ThicknessFactor != thinResult.ThicknessFactor
            || generalResult.EstimatedHardness.MinHB != thinResult.EstimatedHardness.MinHB
            || generalResult.EstimatedHardness.MaxHB != thinResult.EstimatedHardness.MaxHB
            || !generalResult.Flags.SequenceEqual(thinResult.Flags);

        differs.Should().BeTrue("different gray-iron profiles should materially affect the estimate");
    }

    [Fact]
    public void Estimate_WhenProfileBiasChanges_ResultChanges()
    {
        var inputs = CreateValidInputs();
        var baseline = CreateGeneralGrayIronProfile();
        var biased = baseline with { GraphitizationBias = 1.10 };

        var baselineResult = _sut.Estimate(inputs, baseline);
        var biasedResult = _sut.Estimate(inputs, biased);

        biasedResult.GraphitizationScore.Should().NotBe(baselineResult.GraphitizationScore);
    }

    [Fact]
    public void Estimate_WhenCoolingSeverityChanges_ResultChanges()
    {
        var inputs = CreateValidInputs();
        var baseline = CreateGeneralGrayIronProfile();
        var moreSevere = baseline with { CoolingSeverityFactor = 1.20 };

        var baselineResult = _sut.Estimate(inputs, baseline);
        var moreSevereResult = _sut.Estimate(inputs, moreSevere);

        moreSevereResult.CoolingFactor.Should().NotBe(baselineResult.CoolingFactor);
    }

    [Fact]
    public void Estimate_WhenHardnessBandIsTightened_CanProduceFlags()
    {
        var inputs = new CastIronInputs(
            Composition: new CastIronComposition(
                Carbon: 3.20,
                Silicon: 1.90,
                Manganese: 0.90,
                Phosphorus: 0.05,
                Sulfur: 0.02),
            Section: new SectionProfile(
                ThicknessMm: 6.0,
                CoolingRateCPerSec: 2.0));

        var profile = CreateThinSectionGrayIronProfile() with
        {
            HardnessWarningMinBhn = 150,
            HardnessWarningMaxBhn = 180
        };

        var result = _sut.Estimate(inputs, profile);

        result.Flags.Should().NotBeNull();
    }

    [Fact]
    public void Estimate_WhenCarbonEquivalentMovesOutsidePreferredWindow_CanProduceFlags()
    {
        var inputs = new CastIronInputs(
            Composition: new CastIronComposition(
                Carbon: 3.10,
                Silicon: 1.80,
                Manganese: 0.50,
                Phosphorus: 0.02,
                Sulfur: 0.01),
            Section: new SectionProfile(
                ThicknessMm: 40.0,
                CoolingRateCPerSec: 0.20));

        var profile = CreateGeneralGrayIronProfile();

        var result = _sut.Estimate(inputs, profile);

        result.Flags.Should().NotBeNull();
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

    private static CastingProfileDefinition CreateThinSectionGrayIronProfile()
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

    private static CastingProfileDefinition CreateDuctileIronProfile()
        => new(
            Id: "green-sand-ductile-iron",
            DisplayName: "Green Sand - Ductile Iron",
            ProcessFamily: "GreenSand",
            IronType: "DuctileIron",
            DefaultSectionThicknessMm: 25.4,
            CarbonMin: 3.4,
            CarbonMax: 3.9,
            SiliconMin: 2.2,
            SiliconMax: 3.0,
            ManganeseMin: 0.1,
            ManganeseMax: 0.5,
            PhosphorusMin: 0.0,
            PhosphorusMax: 0.08,
            SulfurMin: 0.0,
            SulfurMax: 0.03,
            PreferredCarbonEquivalentMin: 4.2,
            PreferredCarbonEquivalentMax: 4.5,
            GraphitizationBias: 1.0,
            CoolingSeverityFactor: 1.0,
            ChillRiskCeiling: 4.4,
            ShrinkageRiskFloor: 4.0,
            HardnessWarningMinBhn: 140,
            HardnessWarningMaxBhn: 230);
}