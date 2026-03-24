using AutoMapper;
using Castara.Application.DTOs;
using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Application.Mapping;

/// <summary>
/// AutoMapper profile for transforming casting profile configuration DTOs
/// into domain casting profile definitions.
/// </summary>
/// <remarks>
/// This profile maps the hierarchical DTO structure (CastingProfileConfig with nested properties)
/// to a flattened domain model (CastingProfileDefinition) by explicitly mapping each nested
/// property to its corresponding constructor parameter. This approach ensures immutability
/// of the domain model while maintaining a clean separation between the application layer
/// and domain layer concerns.
/// </remarks>
public sealed class CastingProfileMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CastingProfileMappingProfile"/> class
    /// and configures the mapping from <see cref="CastingProfileConfig"/> to <see cref="CastingProfileDefinition"/>.
    /// </summary>
    public CastingProfileMappingProfile()
    {
        CreateMap<CastingProfileConfig, CastingProfileDefinition>()
            // Profile identification and metadata
            .ForCtorParam(
                nameof(CastingProfileDefinition.Id),
                opt => opt.MapFrom(src => src.Id))
            .ForCtorParam(
                nameof(CastingProfileDefinition.DisplayName),
                opt => opt.MapFrom(src => src.DisplayName))
            .ForCtorParam(
                nameof(CastingProfileDefinition.ProcessFamily),
                opt => opt.MapFrom(src => src.ProcessFamily))
            .ForCtorParam(
                nameof(CastingProfileDefinition.IronType),
                opt => opt.MapFrom(src => src.IronType))

            // Default parameters
            .ForCtorParam(
                nameof(CastingProfileDefinition.DefaultSectionThicknessMm),
                opt => opt.MapFrom(src => src.Defaults.SectionThicknessMm))

            // Chemical composition ranges - Carbon
            .ForCtorParam(
                nameof(CastingProfileDefinition.CarbonMin),
                opt => opt.MapFrom(src => src.Ranges.CarbonMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.CarbonMax),
                opt => opt.MapFrom(src => src.Ranges.CarbonMax))

            // Chemical composition ranges - Silicon
            .ForCtorParam(
                nameof(CastingProfileDefinition.SiliconMin),
                opt => opt.MapFrom(src => src.Ranges.SiliconMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.SiliconMax),
                opt => opt.MapFrom(src => src.Ranges.SiliconMax))

            // Chemical composition ranges - Manganese
            .ForCtorParam(
                nameof(CastingProfileDefinition.ManganeseMin),
                opt => opt.MapFrom(src => src.Ranges.ManganeseMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.ManganeseMax),
                opt => opt.MapFrom(src => src.Ranges.ManganeseMax))

            // Chemical composition ranges - Phosphorus
            .ForCtorParam(
                nameof(CastingProfileDefinition.PhosphorusMin),
                opt => opt.MapFrom(src => src.Ranges.PhosphorusMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.PhosphorusMax),
                opt => opt.MapFrom(src => src.Ranges.PhosphorusMax))

            // Chemical composition ranges - Sulfur
            .ForCtorParam(
                nameof(CastingProfileDefinition.SulfurMin),
                opt => opt.MapFrom(src => src.Ranges.SulfurMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.SulfurMax),
                opt => opt.MapFrom(src => src.Ranges.SulfurMax))

            // Target parameters
            .ForCtorParam(
                nameof(CastingProfileDefinition.PreferredCarbonEquivalentMin),
                opt => opt.MapFrom(src => src.Targets.PreferredCarbonEquivalentMin))
            .ForCtorParam(
                nameof(CastingProfileDefinition.PreferredCarbonEquivalentMax),
                opt => opt.MapFrom(src => src.Targets.PreferredCarbonEquivalentMax))
            .ForCtorParam(
                nameof(CastingProfileDefinition.GraphitizationBias),
                opt => opt.MapFrom(src => src.Targets.GraphitizationBias))
            .ForCtorParam(
                nameof(CastingProfileDefinition.CoolingSeverityFactor),
                opt => opt.MapFrom(src => src.Targets.CoolingSeverityFactor))

            // Risk threshold parameters
            .ForCtorParam(
                nameof(CastingProfileDefinition.ChillRiskCeiling),
                opt => opt.MapFrom(src => src.RiskThresholds.ChillRiskCeiling))
            .ForCtorParam(
                nameof(CastingProfileDefinition.ShrinkageRiskFloor),
                opt => opt.MapFrom(src => src.RiskThresholds.ShrinkageRiskFloor))
            .ForCtorParam(
                nameof(CastingProfileDefinition.HardnessWarningMinBhn),
                opt => opt.MapFrom(src => src.RiskThresholds.HardnessWarningMinBhn))
            .ForCtorParam(
                nameof(CastingProfileDefinition.HardnessWarningMaxBhn),
                opt => opt.MapFrom(src => src.RiskThresholds.HardnessWarningMaxBhn));
    }
}