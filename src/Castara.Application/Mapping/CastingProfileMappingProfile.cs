using AutoMapper;
using Castara.Application.DTOs;
using Castara.Domain.Casting;

namespace Castara.Application.Mapping;

/// <summary>
/// AutoMapper profile for transforming casting profile configuration DTOs
/// into domain casting profile definitions.
/// </summary>
public sealed class CastingProfileMappingProfile : Profile
{
    public CastingProfileMappingProfile()
    {
        CreateMap<CastingProfileConfig, CastingProfileDefinition>()
            .ForCtorParam(nameof(CastingProfileDefinition.Id),
                opt => opt.MapFrom(src => src.Id))
            .ForCtorParam(nameof(CastingProfileDefinition.DisplayName),
                opt => opt.MapFrom(src => src.DisplayName))
            .ForCtorParam(nameof(CastingProfileDefinition.ProcessFamily),
                opt => opt.MapFrom(src => src.ProcessFamily))
            .ForCtorParam(nameof(CastingProfileDefinition.IronType),
                opt => opt.MapFrom(src => src.IronType))
            .ForCtorParam(nameof(CastingProfileDefinition.DefaultSectionThicknessMm),
                opt => opt.MapFrom(src => src.Defaults.SectionThicknessMm))
            .ForCtorParam(nameof(CastingProfileDefinition.CarbonMin),
                opt => opt.MapFrom(src => src.Ranges.CarbonMin))
            .ForCtorParam(nameof(CastingProfileDefinition.CarbonMax),
                opt => opt.MapFrom(src => src.Ranges.CarbonMax))
            .ForCtorParam(nameof(CastingProfileDefinition.SiliconMin),
                opt => opt.MapFrom(src => src.Ranges.SiliconMin))
            .ForCtorParam(nameof(CastingProfileDefinition.SiliconMax),
                opt => opt.MapFrom(src => src.Ranges.SiliconMax))
            .ForCtorParam(nameof(CastingProfileDefinition.ManganeseMin),
                opt => opt.MapFrom(src => src.Ranges.ManganeseMin))
            .ForCtorParam(nameof(CastingProfileDefinition.ManganeseMax),
                opt => opt.MapFrom(src => src.Ranges.ManganeseMax))
            .ForCtorParam(nameof(CastingProfileDefinition.PhosphorusMin),
                opt => opt.MapFrom(src => src.Ranges.PhosphorusMin))
            .ForCtorParam(nameof(CastingProfileDefinition.PhosphorusMax),
                opt => opt.MapFrom(src => src.Ranges.PhosphorusMax))
            .ForCtorParam(nameof(CastingProfileDefinition.SulfurMin),
                opt => opt.MapFrom(src => src.Ranges.SulfurMin))
            .ForCtorParam(nameof(CastingProfileDefinition.SulfurMax),
                opt => opt.MapFrom(src => src.Ranges.SulfurMax))
            .ForCtorParam(nameof(CastingProfileDefinition.PreferredCarbonEquivalentMin),
                opt => opt.MapFrom(src => src.Targets.PreferredCarbonEquivalentMin))
            .ForCtorParam(nameof(CastingProfileDefinition.PreferredCarbonEquivalentMax),
                opt => opt.MapFrom(src => src.Targets.PreferredCarbonEquivalentMax))
            .ForCtorParam(nameof(CastingProfileDefinition.GraphitizationBias),
                opt => opt.MapFrom(src => src.Targets.GraphitizationBias))
            .ForCtorParam(nameof(CastingProfileDefinition.CoolingSeverityFactor),
                opt => opt.MapFrom(src => src.Targets.CoolingSeverityFactor))
            .ForCtorParam(nameof(CastingProfileDefinition.ChillRiskCeiling),
                opt => opt.MapFrom(src => src.RiskThresholds.ChillRiskCeiling))
            .ForCtorParam(nameof(CastingProfileDefinition.ShrinkageRiskFloor),
                opt => opt.MapFrom(src => src.RiskThresholds.ShrinkageRiskFloor))
            .ForCtorParam(nameof(CastingProfileDefinition.HardnessWarningMinBhn),
                opt => opt.MapFrom(src => src.RiskThresholds.HardnessWarningMinBhn))
            .ForCtorParam(nameof(CastingProfileDefinition.HardnessWarningMaxBhn),
                opt => opt.MapFrom(src => src.RiskThresholds.HardnessWarningMaxBhn));
    }
}