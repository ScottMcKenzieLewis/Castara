using AutoMapper;
using Castara.Application.DTOs;
using Castara.Domain.Casting;
using Castara.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Castara.Application.Tests.Mapping;

public sealed class CastingProfileConfigToDefinitionMappingTests
{
    private readonly IMapper _mapper = TestMapperFactory.Create();

    [Fact]
    public void Map_ShouldTransformCastingProfileConfig_ToCastingProfileDefinition()
    {
        // Arrange
        var source = new CastingProfileConfig
        {
            Id = "GS_GRAY_30",
            DisplayName = "Green Sand Gray Iron - Class 30",
            ProcessFamily = "GreenSand",
            IronType = "Gray",
            Defaults = new CastingDefaultsConfig
            {
                SectionThicknessMm = 25.4
            },
            Ranges = new CastingRangesConfig
            {
                CarbonMin = 3.2,
                CarbonMax = 3.6,
                SiliconMin = 1.8,
                SiliconMax = 2.4,
                ManganeseMin = 0.6,
                ManganeseMax = 0.9,
                PhosphorusMin = 0.02,
                PhosphorusMax = 0.08,
                SulfurMin = 0.01,
                SulfurMax = 0.06
            },
            Targets = new CastingTargetsConfig
            {
                PreferredCarbonEquivalentMin = 4.0,
                PreferredCarbonEquivalentMax = 4.3,
                GraphitizationBias = 0.15,
                CoolingSeverityFactor = 1.1
            },
            RiskThresholds = new CastingRiskThresholdsConfig
            {
                ChillRiskCeiling = 3.9,
                ShrinkageRiskFloor = 4.35,
                HardnessWarningMinBhn = 170,
                HardnessWarningMaxBhn = 240
            }
        };

        // Act
        var result = _mapper.Map<CastingProfileDefinition>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("GS_GRAY_30");
        result.DisplayName.Should().Be("Green Sand Gray Iron - Class 30");
        result.ProcessFamily.Should().Be("GreenSand");
        result.IronType.Should().Be("Gray");
        result.DefaultSectionThicknessMm.Should().Be(25.4);
        result.CarbonMin.Should().Be(3.2);
        result.CarbonMax.Should().Be(3.6);
        result.SiliconMin.Should().Be(1.8);
        result.SiliconMax.Should().Be(2.4);
        result.ManganeseMin.Should().Be(0.6);
        result.ManganeseMax.Should().Be(0.9);
        result.PhosphorusMin.Should().Be(0.02);
        result.PhosphorusMax.Should().Be(0.08);
        result.SulfurMin.Should().Be(0.01);
        result.SulfurMax.Should().Be(0.06);
        result.PreferredCarbonEquivalentMin.Should().Be(4.0);
        result.PreferredCarbonEquivalentMax.Should().Be(4.3);
        result.GraphitizationBias.Should().Be(0.15);
        result.CoolingSeverityFactor.Should().Be(1.1);
        result.ChillRiskCeiling.Should().Be(3.9);
        result.ShrinkageRiskFloor.Should().Be(4.35);
        result.HardnessWarningMinBhn.Should().Be(170);
        result.HardnessWarningMaxBhn.Should().Be(240);
    }
}